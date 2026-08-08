using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PotionPanic.Editor.Coordination
{
  public sealed class UnityCoordinationWindowPathSource : ICoordinationWindowPathSource
  {
    public bool TryGetActiveStagePath(out string path)
    {
      var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
      path = prefabStage == null ? SceneManager.GetActiveScene().path : prefabStage.assetPath;
      return !string.IsNullOrEmpty(path);
    }

    public bool TryGetProjectSelectionPath(out string path)
    {
      path = Selection.activeObject == null
        ? string.Empty
        : AssetDatabase.GetAssetPath(Selection.activeObject);
      return !string.IsNullOrEmpty(path) && !AssetDatabase.IsValidFolder(path);
    }
  }

  public sealed class UnityCoordinationOverrideConfirmation
    : ICoordinationOverrideConfirmation
  {
    public bool Confirm(string path, string owner)
    {
      return EditorUtility.DisplayDialog(
        "Override coordination claim?",
        path + " is claimed by " + owner
          + ". Override transfers the editing lease to this connection.",
        "Override",
        "Cancel");
    }
  }

  public sealed class CoordinationWindow : EditorWindow
  {
    private CoordinationWindowViewModel viewModel;
    private Vector2 scrollPosition;
    private bool showAdvancedPath;
    private bool clearKeyboardFocus;

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
      clearKeyboardFocus = true;
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
      clearKeyboardFocus = true;
      if (viewModel != null)
      {
        viewModel.Changed += Repaint;
      }
      Repaint();
    }

    private void OnGUI()
    {
      BindViewModel();
      if (clearKeyboardFocus)
      {
        GUI.FocusControl(null);
        GUIUtility.keyboardControl = 0;
        clearKeyboardFocus = false;
      }

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

      var taskContextRect = EditorGUILayout.GetControlRect();
      var taskContextTextRect = EditorGUI.PrefixLabel(taskContextRect,
        new GUIContent("Task context"));
      var taskContext = GUI.TextField(
        taskContextTextRect,
        viewModel.TaskContext,
        CoordinationProtocol.MaximumContextLength);
      if (taskContext != viewModel.TaskContext)
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
      EditorGUILayout.LabelField("Action target",
        string.IsNullOrEmpty(viewModel.SelectedPath) ? "None" : viewModel.SelectedPath);
      using (new EditorGUILayout.HorizontalScope())
      {
        DrawButton("Use active stage", true, viewModel.UseActiveStage);
        DrawButton("Use Project selection", true, viewModel.UseProjectSelection);
      }

      showAdvancedPath = EditorGUILayout.Foldout(
        showAdvancedPath, "Advanced path", true);
      if (showAdvancedPath)
      {
        viewModel.SelectedPath = EditorGUILayout.TextField(
          "Asset path", viewModel.SelectedPath);
      }

      using (new EditorGUILayout.HorizontalScope())
      {
        DrawButton("Reconnect", viewModel.CanReconnect, viewModel.Reconnect);
        DrawButton("Reserve", viewModel.CanReserve, viewModel.Reserve);
        DrawButton("Release editing lease", viewModel.CanRelease, viewModel.Release);
        DrawButton("Cancel reservation",
          viewModel.CanCancelReservation,
          viewModel.CancelReservation);
        DrawButton("Override…", viewModel.CanOverride, viewModel.Override);
      }
      using (new EditorGUILayout.HorizontalScope())
      {
        DrawButton("Copy path",
          viewModel.CanCopyCanonicalPath,
          viewModel.CopyCanonicalPath);
        DrawButton("Forget credentials",
          viewModel.CanForgetCredentials,
          viewModel.ForgetCredentials);
      }
      EditorGUILayout.HelpBox(viewModel.TargetHelpText, MessageType.Info);
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

    private void DrawRows(
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
        var isSelected = viewModel.IsSelected(row);
        var style = new GUIStyle(EditorStyles.helpBox);
        if (isSelected)
        {
          style.normal.background = EditorStyles.selectionRect.normal.background;
        }

        using (var rowScope = new EditorGUILayout.VerticalScope(style))
        {
          EditorGUILayout.LabelField(row.Path, EditorStyles.boldLabel);
          if (isSelected)
          {
            EditorGUILayout.LabelField("Selected action target", EditorStyles.miniLabel);
          }
          EditorGUILayout.LabelField("Owner",
            row.Owner + (row.IsLocal ? " (local)" : string.Empty));
          EditorGUILayout.LabelField("Developer ID", row.DeveloperId);
          EditorGUILayout.LabelField("Branch", EmptyFallback(row.Branch));
          EditorGUILayout.LabelField("Task", EmptyFallback(row.Task));
          EditorGUILayout.LabelField("Expires", EmptyFallback(row.ExpiresAt));
          using (new EditorGUILayout.HorizontalScope())
          {
            if (row.Kind == CoordinationWindowRowKind.EditingLease && row.IsLocal)
            {
              DrawButton("Release editing lease",
                viewModel.CanReleaseRow(row),
                () => viewModel.Release(row));
            }
            else if (row.Kind == CoordinationWindowRowKind.Reservation && row.IsLocal)
            {
              DrawButton("Cancel reservation",
                viewModel.CanCancelReservationRow(row),
                () => viewModel.CancelReservation(row));
            }
            else if (row.Kind != CoordinationWindowRowKind.Presence)
            {
              DrawButton("Override…",
                viewModel.CanOverrideRow(row),
                () => viewModel.Override(row));
            }
            DrawButton("Copy path", true, () => viewModel.CopyPath(row));
          }

          if (Event.current.type == EventType.MouseDown
            && rowScope.rect.Contains(Event.current.mousePosition))
          {
            viewModel.SelectRow(row);
            Repaint();
          }
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
