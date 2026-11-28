using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserEmitter : MonoBehaviour
{
    [SerializeField] private Transform startPoint;
    [SerializeField, Min(0.01f)] private float maximumDistance = 50f;
    [SerializeField, Min(1)] private int maximumInteractions = 20;
    [SerializeField, Min(1)] private int maximumPaths = 16;
    [SerializeField] private LayerMask laserLayers = ~0;
    [SerializeField, Min(0f)] private float surfaceOffset = 0.01f;
    [SerializeField] private ParticleSystem impactEffectPrefab;
    private AudioSource laserAudioSource;
    private AudioClip laserSound;

    private readonly List<List<Vector3>> pathPool = new List<List<Vector3>>();
    private readonly List<LineRenderer> linePool = new List<LineRenderer>();
    private readonly HashSet<LaserReceiver> activeReceivers = new HashSet<LaserReceiver>();
    private readonly HashSet<LaserReceiver> hitReceivers = new HashSet<LaserReceiver>();
    private readonly List<ParticleSystem> impactPool = new List<ParticleSystem>();
    private LineRenderer lineTemplate;
    private int pathCount;
    private int impactCount;

    private void Awake()
    {
        laserSound = GameAudioClips.Instance != null
            ? GameAudioClips.Instance.LaserLoop
            : null;
        lineTemplate = GetComponent<LineRenderer>();
        lineTemplate.useWorldSpace = true;
        linePool.Add(lineTemplate);
    }

    private void OnEnable()
    {
        if (laserAudioSource == null && laserSound != null)
            laserAudioSource = PlayerAudioController.CreatePlayerSource();

        if (laserAudioSource == null || laserSound == null)
            return;

        laserAudioSource.clip = laserSound;
        laserAudioSource.loop = true;
        laserAudioSource.Play();
    }

    private void LateUpdate()
    {
        BuildLaser();
        UpdateLines();
        UpdateReceivers();
    }

    private void BuildLaser()
    {
        pathCount = 0;
        impactCount = 0;
        hitReceivers.Clear();

        List<Vector3> firstPath = GetPath();
        Vector3 origin = startPoint != null ? startPoint.position : transform.position;
        Vector3 direction = startPoint != null ? startPoint.forward : transform.forward;

        if (direction.sqrMagnitude < 0.000001f)
            direction = transform.forward;

        firstPath.Add(origin);
        Trace(origin, direction.normalized, firstPath, 0);
    }

    private void Trace(Vector3 origin, Vector3 direction, List<Vector3> path, int interactionCount)
    {
        float distance = Mathf.Clamp(
            SaveDataValidator.Finite(maximumDistance, 50f),
            0.01f,
            10000f
        );

        if (direction.sqrMagnitude < 0.000001f)
        {
            path.Add(origin);
            return;
        }

        direction.Normalize();

        if (interactionCount >= Mathf.Clamp(maximumInteractions, 1, 100))
        {
            path.Add(origin + direction * distance);
            return;
        }

        if (!Physics.Raycast(origin, direction, out RaycastHit hit, distance, laserLayers))
        {
            path.Add(origin + direction * distance);
            return;
        }

        path.Add(hit.point);
        AddImpact(hit.point, hit.normal);

        LaserReceiver receiver = hit.collider.GetComponentInParent<LaserReceiver>();

        if (receiver != null)
        {
            hitReceivers.Add(receiver);
            return;
        }

        LaserReflectiveSurface reflectiveSurface = hit.collider.GetComponent<LaserReflectiveSurface>();

        if (reflectiveSurface != null)
        {
            Vector3 reflectedDirection = Vector3.Reflect(direction, hit.normal).normalized;
            Vector3 reflectedOrigin = hit.point + reflectedDirection * Mathf.Max(0f, surfaceOffset);
            Trace(reflectedOrigin, reflectedDirection, path, interactionCount + 1);
            return;
        }

        LaserRedirectorCube redirector = hit.collider.GetComponentInParent<LaserRedirectorCube>();

        if (redirector == null)
            return;

        Vector3 outputPosition = redirector.OutputPosition;
        path.Add(outputPosition);

        if (redirector.TryGetMountedMirror(out LaserMirror mirror))
        {
            Vector3 splitPosition = mirror.SplitPosition;
            Vector3 reflectedDirection = -mirror.GetSplitDirection(redirector.OutputDirection);

            path.Add(splitPosition);
            AddImpact(splitPosition, mirror.ReflectionNormal);

            Trace(
                splitPosition + reflectedDirection * Mathf.Max(0f, surfaceOffset),
                reflectedDirection,
                path,
                interactionCount + 1
            );

            return;
        }

        Vector3 outputDirection = redirector.OutputDirection.normalized;
        Vector3 nextOrigin = outputPosition + outputDirection * Mathf.Max(0f, surfaceOffset);
        Trace(nextOrigin, outputDirection, path, interactionCount + 1);
    }

    private List<Vector3> GetPath()
    {
        if (pathCount >= Mathf.Clamp(maximumPaths, 1, 64))
            return null;

        if (pathCount >= pathPool.Count)
            pathPool.Add(new List<Vector3>(16));

        List<Vector3> path = pathPool[pathCount];
        path.Clear();
        pathCount++;
        return path;
    }

    private void UpdateLines()
    {
        while (linePool.Count < pathCount)
            linePool.Add(CreateLineRenderer());

        for (int i = 0; i < linePool.Count; i++)
        {
            bool shouldBeVisible = i < pathCount;
            linePool[i].enabled = shouldBeVisible;

            if (!shouldBeVisible)
                continue;

            List<Vector3> path = pathPool[i];
            linePool[i].positionCount = path.Count;

            for (int pointIndex = 0; pointIndex < path.Count; pointIndex++)
                linePool[i].SetPosition(pointIndex, path[pointIndex]);
        }

        UpdateImpactPool();
    }

    private void AddImpact(Vector3 position, Vector3 normal)
    {
        if (impactEffectPrefab == null)
            return;

        while (impactPool.Count <= impactCount)
        {
            ParticleSystem effect = Instantiate(impactEffectPrefab, transform);
            effect.gameObject.SetActive(false);
            impactPool.Add(effect);
        }

        ParticleSystem impact = impactPool[impactCount];
        impact.transform.SetPositionAndRotation(
            position + normal * Mathf.Max(0f, surfaceOffset),
            normal.sqrMagnitude > 0.000001f
                ? Quaternion.LookRotation(normal)
                : Quaternion.identity
        );

        if (!impact.gameObject.activeSelf)
        {
            impact.gameObject.SetActive(true);
            impact.Play(true);
        }

        impactCount++;
    }

    private void UpdateImpactPool()
    {
        for (int i = impactCount; i < impactPool.Count; i++)
        {
            ParticleSystem impact = impactPool[i];

            if (!impact.gameObject.activeSelf)
                continue;

            impact.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            impact.gameObject.SetActive(false);
        }
    }

    private LineRenderer CreateLineRenderer()
    {
        GameObject lineObject = new GameObject("Laser Path");
        lineObject.transform.SetParent(transform);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.sharedMaterial = lineTemplate.sharedMaterial;
        line.widthMultiplier = lineTemplate.widthMultiplier;
        line.widthCurve = lineTemplate.widthCurve;
        line.colorGradient = lineTemplate.colorGradient;
        line.textureMode = lineTemplate.textureMode;
        line.alignment = lineTemplate.alignment;
        line.numCapVertices = lineTemplate.numCapVertices;
        line.numCornerVertices = lineTemplate.numCornerVertices;
        line.shadowCastingMode = lineTemplate.shadowCastingMode;
        line.receiveShadows = lineTemplate.receiveShadows;
        return line;
    }

    private void UpdateReceivers()
    {
        foreach (LaserReceiver receiver in activeReceivers)
        {
            if (receiver != null && !hitReceivers.Contains(receiver))
                receiver.StopReceiving(this);
        }

        foreach (LaserReceiver receiver in hitReceivers)
        {
            if (receiver != null && !activeReceivers.Contains(receiver))
                receiver.Receive(this);
        }

        activeReceivers.Clear();

        foreach (LaserReceiver receiver in hitReceivers)
            activeReceivers.Add(receiver);
    }

    private void OnDisable()
    {
        if (laserAudioSource != null)
            laserAudioSource.Stop();

        foreach (LaserReceiver receiver in activeReceivers)
        {
            if (receiver != null)
                receiver.StopReceiving(this);
        }

        activeReceivers.Clear();

        foreach (LineRenderer line in linePool)
        {
            if (line != null)
                line.enabled = false;
        }

        foreach (ParticleSystem impact in impactPool)
        {
            if (impact == null)
                continue;

            impact.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            impact.gameObject.SetActive(false);
        }
    }
}
