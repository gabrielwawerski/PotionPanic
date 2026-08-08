using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PotionPanic.Editor.Coordination
{
  public sealed class CoordinationWindow : EditorWindow
  {
    private CoordinationWindowViewModel viewModel;
    private Vector2 scrollPosition;

    [MenuItem("Window/Potion Panic/Coordination")]
    public static void ShowWindow()
    {
      var window = GetWindow<CoordinationWindow>();
      window.titleContent = new GUIContent("Coordination");
      window.minSize = new Vector2(560, 420);
      window.Show();
    }

    public static bool TryPublishNotification(CoordinationNotification notification)
    {
      if (notification == null || string.IsNullOrEmpty(notification.Message))
      {
        return true;
      }

      var published = false;
      foreach (var window in Resources.FindObjectsOfTypeAll<CoordinationWindow>())
      {
        window.ShowNotification(new GUIContent(notification.Message));
        window.Repaint();
        published = true;
      }
      return published;
    }

    private void OnEnable()
    {
      CoordinationEditorBootstrap.ReconnectRuntime();
      CoordinationEditorBootstrap.FlushPendingNotifications();
      EditorApplication.update -= BindViewModel;
      EditorApplication.update += BindViewModel;
      BindViewModel();
    }

    private void OnDisable()
    {
      EditorApplication.update -= BindViewModel;
      if (viewModel != null)
      {
        viewModel.Changed -= Repaint;
        viewModel = null;
      }
    }

    private void BindViewModel()
    {
      var current = CoordinationEditorBootstrap.ViewModel;
      if (ReferenceEquals(viewModel, current))
      {
        return;
      }

      if (viewModel != null)
      {
        viewModel.Changed -= Repaint;
      }

      viewModel = current;
      if (viewModel != null)
      {
        viewModel.Changed += Repaint;
      }
      Repaint();
    }

    private void OnGUI()
    {
      BindViewModel();
      if (viewModel == null)
      {
        EditorGUILayout.HelpBox(
          "Coordination is starting. Check the Console if this state persists.",
          MessageType.Info);
        return;
      }

      scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
      DrawStatus();
      DrawWarnings();
      DrawActions();
      DrawRows("Presence", viewModel.Presence, "No coordinated assets are open.");
      DrawRows("Editing leases", viewModel.EditingLeases, "No editing leases.");
      DrawRows("Reservations", viewModel.Reservations, "No reservations.");
      EditorGUILayout.EndScrollView();
    }

    private void DrawStatus()
    {
      EditorGUILayout.LabelField("Local", EditorStyles.boldLabel);
      EditorGUILayout.LabelField("Identity", viewModel.Identity);
      EditorGUILayout.LabelField("Git branch",
        string.IsNullOrEmpty(viewModel.Branch) ? "Unavailable" : viewModel.Branch);
      EditorGUILayout.LabelField("Connection", viewModel.ConnectionState.ToString());

      EditorGUI.BeginChangeCheck();
      var taskContext = EditorGUILayout.TextField("Task context", viewModel.TaskContext);
      if (EditorGUI.EndChangeCheck())
      {
        viewModel.TaskContext = taskContext;
      }

      using (new EditorGUI.DisabledScope(!viewModel.CanEditDisabled))
      {
        EditorGUI.BeginChangeCheck();
        var disabled = EditorGUILayout.Toggle("Disabled", viewModel.IsDisabled);
        if (EditorGUI.EndChangeCheck())
        {
          viewModel.SetDisabled(disabled);
        }
      }

      if (!viewModel.CanEditDisabled)
      {
        EditorGUILayout.HelpBox(
          "Coordination is available only in the Windows editor.",
          MessageType.Info);
      }
      EditorGUILayout.Space();
    }

    private void DrawWarnings()
    {
      foreach (var warning in viewModel.Warnings)
      {
        foreach (var path in warning.PathDetails)
        {
          var owner = string.IsNullOrEmpty(path.LastKnownOwner)
            ? "unknown owner"
            : path.LastKnownOwner;
          EditorGUILayout.HelpBox(
            path.Path + " was saved without coordination. Last known owner: " + owner + ".",
            MessageType.Warning);
        }
      }
    }

    private void DrawActions()
    {
      EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
      viewModel.SelectedPath = EditorGUILayout.TextField(
        "Asset path", viewModel.SelectedPath);
      using (new EditorGUILayout.HorizontalScope())
      {
        DrawButton("Reconnect", viewModel.CanReconnect, viewModel.Reconnect);
        DrawButton("Reserve", viewModel.CanReserve, viewModel.Reserve);
        DrawButton("Release", viewModel.CanRelease, viewModel.Release);
        DrawButton("Override", viewModel.CanOverride, viewModel.Override);
      }
      using (new EditorGUILayout.HorizontalScope())
      {
        DrawButton("Copy canonical path",
          viewModel.CanCopyCanonicalPath,
          viewModel.CopyCanonicalPath);
        DrawButton("Forget credentials",
          viewModel.CanForgetCredentials,
          viewModel.ForgetCredentials);
      }
      EditorGUILayout.Space();
    }

    private static void DrawButton(string label, bool enabled, Func<bool> action)
    {
      using (new EditorGUI.DisabledScope(!enabled))
      {
        if (GUILayout.Button(label))
        {
          action();
        }
      }
    }

    private static void DrawRows(
      string title,
      IReadOnlyList<CoordinationWindowRow> rows,
      string emptyMessage)
    {
      EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
      if (rows.Count == 0)
      {
        EditorGUILayout.LabelField(emptyMessage, EditorStyles.miniLabel);
        EditorGUILayout.Space();
        return;
      }

      foreach (var row in rows)
      {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
          EditorGUILayout.LabelField(row.Path, EditorStyles.boldLabel);
          EditorGUILayout.LabelField("Owner",
            row.Owner + (row.IsLocal ? " (local)" : string.Empty));
          EditorGUILayout.LabelField("Developer ID", row.DeveloperId);
          EditorGUILayout.LabelField("Branch", EmptyFallback(row.Branch));
          EditorGUILayout.LabelField("Task", EmptyFallback(row.Task));
          EditorGUILayout.LabelField("Expires", EmptyFallback(row.ExpiresAt));
        }
      }
      EditorGUILayout.Space();
    }

    private static string EmptyFallback(string value)
    {
      return string.IsNullOrEmpty(value) ? "None" : value;
    }
  }
}
