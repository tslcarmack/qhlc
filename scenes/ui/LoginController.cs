using System;
using System.IO;
using Godot;
using Qhlc.Core.Nts;

namespace Qhlc.Game;

public partial class LoginController : Control
{
    private const string ShellNodePath = "LoginShell";
    private const string EnterButtonPath = "LoginShell/Content/EnterButton";

    private static readonly string[] PanelCandidates =
    {
        "STARTUP.bmp",
        "Form_NewRole_3.bmp",
        "Form_changePassWord.bmp",
        "form_deleteRole.bmp",
    };

    [Signal]
    public delegate void EnterGameEventHandler();

    public string AssetsRoot { get; set; } = "";

    public override void _Ready()
    {
        var shell = GetNode<SkinWindow>(ShellNodePath);
        shell.EnableDrag = false;
        shell.EnableClose = false;

        var button = GetNode<Button>(EnterButtonPath);
        button.Pressed += OnEnterPressed;

        var background = shell.GetNode<TextureRect>("Background");
        if (background.Texture is null && !TryApplyPanelBackgroundFallback(background))
            ShowFallbackBackground();
    }

    private void OnEnterPressed() => EmitSignal(SignalName.EnterGame);

    private bool TryApplyPanelBackgroundFallback(TextureRect background)
    {
        var skinDir = Path.Combine(AssetsRoot, "assets", "pic");
        if (!Directory.Exists(skinDir))
            return false;

        foreach (var name in PanelCandidates)
        {
            var path = Path.Combine(skinDir, name);
            if (!File.Exists(path))
                continue;

            try
            {
                var bytes = File.ReadAllBytes(path);
                var raster = NtsBmp.Decode(bytes);
                var image = Image.CreateFromData(
                    raster.Width,
                    raster.Height,
                    false,
                    Image.Format.Rgba8,
                    raster.Rgba);
                background.Texture = ImageTexture.CreateFromImage(image);
                background.Visible = true;
                return true;
            }
            catch (Exception ex)
            {
                GD.Print($"Login: BMP fallback load failed ({name}): {ex.Message}");
            }
        }

        return false;
    }

    private void ShowFallbackBackground()
    {
        var shell = GetNode<SkinWindow>(ShellNodePath);
        shell.GetNode<TextureRect>("Background").Visible = false;

        var fallback = GetNode<ColorRect>("FallbackBackground");
        fallback.Visible = true;
    }
}
