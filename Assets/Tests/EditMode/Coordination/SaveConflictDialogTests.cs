using System.Collections.Generic;
using NUnit.Framework;
using PotionPanic.Editor.Coordination;

namespace PotionPanic.Tests.EditMode.Coordination
{
  public sealed class SaveConflictDialogTests
  {
    [TestCase(0, SaveConflictAction.OverrideAndSave)]
    [TestCase(1, SaveConflictAction.CancelSave)]
    [TestCase(2, SaveConflictAction.KeepWorking)]
    public void MapsExactlyThreeConflictButtonsToTheirActions(
      int selectedButton,
      SaveConflictAction expected)
    {
      var backend = new FakeDialogBackend { ComplexResult = selectedButton };
      var dialog = new SaveConflictDialog(backend);

      var result = dialog.Show(new[]
      {
        new CoordinationSavePathInfo("Assets/Scenes/Laboratory.unity", "Morgan")
      });

      Assert.That(result, Is.EqualTo(expected));
      Assert.That(backend.ComplexButtons, Is.EqualTo(new[]
      {
        "Override and save", "Cancel save", "Keep working"
      }));
    }

    [Test]
    public void SecondLocalSaveConfirmationShowsEveryPathAndLastKnownOwner()
    {
      var backend = new FakeDialogBackend { ConfirmResult = true };
      var prompt = new UncoordinatedSavePrompt(backend);
      var paths = new[]
      {
        new CoordinationSavePathInfo("Assets/Scenes/Laboratory.unity", "Morgan"),
        new CoordinationSavePathInfo("Assets/Scenes/Arena.unity", "No owner known")
      };

      var result = prompt.ConfirmLocalSave(paths);

      Assert.That(result, Is.True);
      Assert.That(backend.ConfirmMessage, Does.Contain("Assets/Scenes/Laboratory.unity"));
      Assert.That(backend.ConfirmMessage, Does.Contain("Morgan"));
      Assert.That(backend.ConfirmMessage, Does.Contain("Assets/Scenes/Arena.unity"));
      Assert.That(backend.ConfirmMessage, Does.Contain("No owner known"));
    }

    [Test]
    public void FirstLocalSaveChoiceUsesTheRequiredActionLabel()
    {
      var backend = new FakeDialogBackend { ConfirmResult = true };
      var prompt = new UncoordinatedSavePrompt(backend);

      prompt.ChooseLocalSave(new[]
      {
        new CoordinationSavePathInfo("Assets/Scenes/Laboratory.unity", "Morgan")
      });

      Assert.That(backend.ConfirmButton, Is.EqualTo("Save locally without coordination"));
    }

    private sealed class FakeDialogBackend : ICoordinationEditorDialogBackend
    {
      public int ComplexResult { get; set; }
      public bool ConfirmResult { get; set; }
      public IReadOnlyList<string> ComplexButtons { get; private set; }
      public string ConfirmMessage { get; private set; }
      public string ConfirmButton { get; private set; }

      public int ShowComplex(
        string title,
        string message,
        string primary,
        string cancel,
        string alternate)
      {
        ComplexButtons = new[] { primary, cancel, alternate };
        return ComplexResult;
      }

      public bool ShowConfirmation(
        string title,
        string message,
        string confirm,
        string cancel)
      {
        ConfirmMessage = message;
        ConfirmButton = confirm;
        return ConfirmResult;
      }
    }
  }
}
