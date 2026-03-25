using System;

namespace Swarm.Node.BackgroundServices;

/// <summary>
/// Class that orchestrate the execution order of background services
/// </summary>
public class BackgroundMaestro
{
    private BackgroundMaestro? _instance;
    private readonly Semaphore _semaphore;

    private BackgroundMaestro()
    {
        _semaphore = new Semaphore(1,1);
    }

    public BackgroundMaestro getInstance()
    {
        _instance ??= new BackgroundMaestro(); 
        return _instance;
    }

    public void AcquireLock()
    {
        var a = _semaphore.WaitOne();
    }


}
