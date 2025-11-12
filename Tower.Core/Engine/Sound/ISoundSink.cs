namespace Tower.Core.Engine.Sound;

public interface ISoundSink
{
 int Play(string logicalId, bool loop = false, float volume =1f, float pitch =0f, float pan =0f);
 void Stop(int handle);
 void SetVolume(int handle, float vol);
}
