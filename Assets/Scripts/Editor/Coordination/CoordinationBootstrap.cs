using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace PotionPanic.Editor.Coordination
{
  public interface ICoordinationEditorRuntime
  {
    CoordinationWindowViewModel ViewModel { get; }
    Task StartAsync();
    Task ShutdownAsync();
    void FlushPendingNotifications();
  }

  public interface ICoordinationEditorLifecycleHooks
  {
    event Action Reloading;
    event Action Restarting;
    event Action ShuttingDown;
  }

  public sealed class CoordinationBootstrapController
  {
    private readonly ICoordinationEditorLifecycleHooks hooks;
    private readonly Func<ICoordinationEditorRuntime> runtimeFactory;
    private readonly object lifecycleLock = new object();
    private ICoordinationEditorRuntime runtime;
    private Task startTask;
    private Task shutdownTask;
    private bool isEnabled;

    public CoordinationWindowViewModel ViewModel
    {
      get
      {
        lock (lifecycleLock)
        {
          return runtime?.ViewModel;
        }
      }
    }

    public CoordinationBootstrapController(
      ICoordinationEditorLifecycleHooks hooks,
      Func<ICoordinationEditorRuntime> runtimeFactory)
    {
      this.hooks = hooks ?? throw new ArgumentNullException(nameof(hooks));
      this.runtimeFactory = runtimeFactory
        ?? throw new ArgumentNullException(nameof(runtimeFactory));
    }

    public void Enable()
    {
      if (isEnabled)
      {
        return;
      }

      hooks.Reloading += HandleShutdown;
      hooks.Restarting += HandleRestart;
      hooks.ShuttingDown += HandleShutdown;
      isEnabled = true;
    }

    public void Disable()
    {
      if (!isEnabled)
      {
        return;
      }

      hooks.Reloading -= HandleShutdown;
      hooks.Restarting -= HandleRestart;
      hooks.ShuttingDown -= HandleShutdown;
      isEnabled = false;
    }

    public Task StartAsync()
    {
      lock (lifecycleLock)
      {
        if (runtime != null)
        {
          return startTask ?? Task.CompletedTask;
        }

        runtime = runtimeFactory();
        startTask = runtime.StartAsync();
        return startTask;
      }
    }

    public Task ShutdownAsync()
    {
      lock (lifecycleLock)
      {
        if (runtime == null)
        {
          return shutdownTask ?? Task.CompletedTask;
        }

        var current = runtime;
        var currentStart = startTask;
        runtime = null;
        startTask = null;
        shutdownTask = ShutdownRuntimeAsync(current, currentStart);
        return shutdownTask;
      }
    }

    public void FlushPendingNotifications()
    {
      lock (lifecycleLock)
      {
        runtime?.FlushPendingNotifications();
      }
    }

    private static async Task ShutdownRuntimeAsync(
      ICoordinationEditorRuntime runtime,
      Task startTask)
    {
      if (startTask != null)
      {
        _ = ObserveStartupAsync(startTask);
      }

      await runtime.ShutdownAsync().ConfigureAwait(false);
    }

    private static async Task ObserveStartupAsync(Task startTask)
    {
      try
      {
        await startTask.ConfigureAwait(false);
      }
      catch
      {
        // Runtime shutdown owns cleanup for partially completed startup.
      }
    }

    private void HandleShutdown()
    {
      var synchronizationContext = SynchronizationContext.Current;
      try
      {
        SynchronizationContext.SetSynchronizationContext(null);
        ShutdownAsync().GetAwaiter().GetResult();
      }
      finally
      {
        SynchronizationContext.SetSynchronizationContext(synchronizationContext);
      }
    }

    private async void HandleRestart()
    {
      try
      {
        await StartAsync();
      }
      catch (Exception exception)
      {
        Debug.LogException(exception);
      }
    }
  }

  public sealed class UnityCoordinationEditorLifecycleHooks
    : ICoordinationEditorLifecycleHooks,
      IDisposable
  {
    public event Action Reloading;
    public event Action Restarting;
    public event Action ShuttingDown;

    public UnityCoordinationEditorLifecycleHooks()
    {
      AssemblyReloadEvents.beforeAssemblyReload += OnReloading;
      CompilationPipeline.compilationStarted += OnCompilationStarted;
      CompilationPipeline.compilationFinished += OnCompilationFinished;
      EditorApplication.quitting += OnShuttingDown;
    }

    public void Dispose()
    {
      AssemblyReloadEvents.beforeAssemblyReload -= OnReloading;
      CompilationPipeline.compilationStarted -= OnCompilationStarted;
      CompilationPipeline.compilationFinished -= OnCompilationFinished;
      EditorApplication.delayCall -= OnCompilationRestarting;
      EditorApplication.quitting -= OnShuttingDown;
    }

    private void OnReloading()
    {
      EditorApplication.delayCall -= OnCompilationRestarting;
      Reloading?.Invoke();
    }

    private void OnCompilationStarted(object _)
    {
      EditorApplication.delayCall -= OnCompilationRestarting;
      Reloading?.Invoke();
    }
    private void OnCompilationFinished(object _)
    {
      EditorApplication.delayCall -= OnCompilationRestarting;
      EditorApplication.delayCall += OnCompilationRestarting;
    }

    private void OnCompilationRestarting()
    {
      EditorApplication.delayCall -= OnCompilationRestarting;
      Restarting?.Invoke();
    }

    private void OnShuttingDown()
    {
      EditorApplication.delayCall -= OnCompilationRestarting;
      ShuttingDown?.Invoke();
    }
  }

  public interface ICoordinationUnityNotificationPresenter
  {
    bool TryPublish(CoordinationNotification notification);
  }

  public sealed class CoordinationWindowNotificationPresenter
    : ICoordinationUnityNotificationPresenter
  {
    public bool TryPublish(CoordinationNotification notification)
      => CoordinationWindow.TryPublishNotification(notification);
  }

  public sealed class UnityCoordinationNotificationSink
    : ICoordinationNotificationSink
  {
    private const int MaxPendingNotifications = 20;
    private readonly ICoordinationUnityNotificationPresenter presenter;
    private readonly Queue<CoordinationNotification> pending
      = new Queue<CoordinationNotification>();

    public int PendingCount => pending.Count;

    public UnityCoordinationNotificationSink()
      : this(new CoordinationWindowNotificationPresenter())
    {
    }

    public UnityCoordinationNotificationSink(
      ICoordinationUnityNotificationPresenter presenter)
    {
      this.presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
    }

    public void Publish(CoordinationNotification notification)
    {
      if (notification == null || presenter.TryPublish(notification))
      {
        return;
      }

      if (pending.Count == MaxPendingNotifications)
      {
        pending.Dequeue();
      }
      pending.Enqueue(notification);
    }

    public void FlushPending()
    {
      while (pending.Count > 0 && presenter.TryPublish(pending.Peek()))
      {
        pending.Dequeue();
      }
    }
  }

  public sealed class CoordinationEditorRuntime : ICoordinationEditorRuntime
  {
    private static readonly TimeSpan SaveRequestTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ProlongedDisconnect = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ShutdownSendTimeout = TimeSpan.FromSeconds(2);

    private readonly CoordinationService service;
    private readonly CoordinationStageLifecycleAdapter lifecycle;
    private readonly CoordinationAssetTracker tracker;
    private readonly CoordinationSaveResumeCoordinator saveCoordinator;
    private readonly CoordinationSavePathFilter saveFilter;
    private readonly CoordinationNotificationController notifications;
    private readonly UnityCoordinationNotificationSink notificationSink;
    private bool started;
    private bool shutDown;

    public CoordinationWindowViewModel ViewModel { get; }

    private CoordinationEditorRuntime(
      CoordinationService service,
      CoordinationStageLifecycleAdapter lifecycle,
      CoordinationAssetTracker tracker,
      CoordinationSaveResumeCoordinator saveCoordinator,
      CoordinationSavePathFilter saveFilter,
      CoordinationNotificationController notifications,
      UnityCoordinationNotificationSink notificationSink,
      CoordinationWindowViewModel viewModel)
    {
      this.service = service;
      this.lifecycle = lifecycle;
      this.tracker = tracker;
      this.saveCoordinator = saveCoordinator;
      this.saveFilter = saveFilter;
      this.notifications = notifications;
      this.notificationSink = notificationSink;
      ViewModel = viewModel;
    }

    public static bool TryCreateDefault(out ICoordinationEditorRuntime runtime, out string error)
    {
      runtime = null;
      if (!CoordinationConfig.TryLoad(out var configuration, out error)
        || !CoordinationUserSettings.TryLoad(out var settings, out error))
      {
        return false;
      }

      var credentialStore = new WindowsCredentialStore();
      var gitContext = new GitCoordinationContext();
      var service = new CoordinationService(
        configuration,
        settings,
        credentialStore,
        new UnityWebRequestCoordinationHttpClient(),
        new ClientWebSocketCoordinationClient(),
        new UnityMainThreadDispatcher(),
        gitContext,
        Application.platform == RuntimePlatform.WindowsEditor,
        onSaved => CoordinationCredentialWindow.ShowForProject(
          configuration.projectId, credentialStore, onSaved));
      var lifecycle = new CoordinationStageLifecycleAdapter(
        new UnityCoordinationStageLifecycleSource());
      var stateStore = new CoordinationStateStore();
      var warnings = new CoordinationUncoordinatedSaveState();
      var tracker = new CoordinationAssetTracker(
        lifecycle, service, configuration.rules, stateStore);
      var saveCoordinator = new CoordinationSaveResumeCoordinator(
        service,
        stateStore,
        warnings,
        new UnityCoordinationSaveScheduler(),
        new SaveConflictDialog(),
        new UncoordinatedSavePrompt(),
        new UnityCoordinationSaveInvoker(),
        new UnityCoordinationSaveWarningLogger(),
        gitContext,
        () => settings.taskContext,
        SaveRequestTimeout);
      var saveFilter = new CoordinationSavePathFilter(
        saveCoordinator, configuration.rules, new UnityCoordinationSaveScheduler());
      var notificationSink = new UnityCoordinationNotificationSink();
      var notifications = new CoordinationNotificationController(
        service,
        notificationSink,
        new SystemCoordinationClock(),
        ProlongedDisconnect);
      var viewModel = new CoordinationWindowViewModel(
        service,
        stateStore,
        warnings,
        settings,
        new UnityCoordinationUserSettingsStore(),
        configuration.rules,
        gitContext,
        new UnityCoordinationClipboard(),
        new UnityCoordinationWindowPathSource(),
        new UnityCoordinationOverrideConfirmation(),
        new UnityCoordinationWindowConfirmation());
      runtime = new CoordinationEditorRuntime(
        service,
        lifecycle,
        tracker,
        saveCoordinator,
        saveFilter,
        notifications,
        notificationSink,
        viewModel);
      error = null;
      return true;
    }

    public async Task StartAsync()
    {
      if (started || shutDown)
      {
        return;
      }

      started = true;
      tracker.Enable();
      saveCoordinator.Enable();
      CoordinationSaveGuard.Install(saveFilter);
      ViewModel.Enable();
      notifications.Enable();
      EditorApplication.update += notifications.Tick;
      await service.ConnectAsync();
    }

    public async Task ShutdownAsync()
    {
      if (shutDown)
      {
        return;
      }

      shutDown = true;
      EditorApplication.update -= notifications.Tick;
      notifications.Disable();
      CoordinationSaveGuard.Uninstall(saveFilter);
      saveCoordinator.Disable();
      ViewModel.Disable();
      if (started)
      {
        tracker.ReleaseOwnedCoordination();
      }
      tracker.Disable();
      lifecycle.Dispose();
      if (started)
      {
        // Keep cancellation on Unity's main thread after the bounded release flush.
        // FlushPendingSendsAsync never captures the synchronization context.
        service.FlushPendingSendsAsync(ShutdownSendTimeout)
          .GetAwaiter().GetResult();
      }
      await service.ShutdownAsync().ConfigureAwait(false);
    }

    public void FlushPendingNotifications()
    {
      notificationSink.FlushPending();
    }
  }

  [InitializeOnLoad]
  public static class CoordinationEditorBootstrap
  {
    private static readonly UnityCoordinationEditorLifecycleHooks Hooks;
    private static readonly CoordinationBootstrapController Controller;

    public static CoordinationWindowViewModel ViewModel => Controller?.ViewModel;

    static CoordinationEditorBootstrap()
    {
      if (Application.isBatchMode)
      {
        return;
      }

      Hooks = new UnityCoordinationEditorLifecycleHooks();
      Controller = new CoordinationBootstrapController(Hooks, CreateRuntime);
      Controller.Enable();
      EditorApplication.delayCall += Start;
    }

    public static void ReconnectRuntime()
    {
      if (Controller != null)
      {
        _ = ObserveAsync(Controller.StartAsync());
      }
    }

    public static void FlushPendingNotifications()
    {
      Controller?.FlushPendingNotifications();
    }

    private static ICoordinationEditorRuntime CreateRuntime()
    {
      if (!CoordinationEditorRuntime.TryCreateDefault(out var runtime, out var error))
      {
        throw new InvalidOperationException(error);
      }

      return runtime;
    }

    private static void Start()
    {
      if (Controller != null)
      {
        _ = ObserveAsync(Controller.StartAsync());
      }
    }

    private static async Task ObserveAsync(Task task)
    {
      try
      {
        await task;
      }
      catch (Exception exception)
      {
        Debug.LogException(exception);
      }
    }
  }
}
