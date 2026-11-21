public interface IJsonSaveable
{
    string CaptureJsonState();
    void RestoreJsonState(string json);
}
