﻿using ClickableTransparentOverlay;
using ImGuiNET;
using System.Runtime.InteropServices;

namespace QuickInstall
{
    class UIManager : Overlay
    {
        private ProgramManager programManager;
        private int progress = 0;
        private string status = "Idle";

        public UIManager(ProgramManager programManager) : base(GetScreenWidth(), GetScreenHeight())
        {
            this.programManager = programManager;
            programManager.ProgressChanged += (p) => progress = p;
            programManager.StatusChanged += (s) => status = s;
        }

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        public static int GetScreenWidth() => GetSystemMetrics(0);
        public static int GetScreenHeight() => GetSystemMetrics(1);

        protected override void Render()
        {
            ImGui.Begin("QuickInstall", ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoBringToFrontOnFocus);

            RenderMenuBar();
            RenderArchitectureSelection();
            RenderInstallAfter();
            RenderTagFilter();
            RenderProgramList();
            RenderInstallButton();
            RenderProgressBar();
            RenderStatusBar();

            ImGui.End();
        }

        private void RenderMenuBar()
        {
            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Exit"))
                    {
                        Environment.Exit(0);
                    }
                    ImGui.EndMenu();
                }
                ImGui.EndMenuBar();
            }
        }

        private void RenderArchitectureSelection()
        {
            ImGui.Text("Select Architecture:");
            if (ImGui.RadioButton("32-bit", programManager.SelectedArchitecture == "32-bit"))
            {
                programManager.SelectedArchitecture = "32-bit";
            }
            ImGui.SameLine();
            if (ImGui.RadioButton("64-bit", programManager.SelectedArchitecture == "64-bit"))
            {
                programManager.SelectedArchitecture = "64-bit";
            }
        }

        private void RenderTagFilter()
        {
            ImGui.Text("Filter by tag:");
            foreach (var tag in programManager.Tags)
            {
                if (ImGui.Button(tag.ToUpper()))
                {
                    programManager.SelectedTag = tag;
                }
                ImGui.SameLine();
            }
            ImGui.NewLine();
        }

        private void RenderProgramList()
        {
            foreach (var program in programManager.Programs)
            {
                if (programManager.SelectedTag == "all" || program.Tags.Contains(programManager.SelectedTag.ToLower()))
                {
                    ImGui.Checkbox(program.Name, ref program.IsSelected);
                }
            }
        }

        private void RenderInstallAfter()
        {
            bool installAfterDownload = programManager.IsInstallAfterDownload;
            if (ImGui.Checkbox("Install after download", ref installAfterDownload))
            {
                programManager.IsInstallAfterDownload = installAfterDownload;
            }
        }


        private void RenderInstallButton()
        {
            if (ImGui.Button(programManager.IsInstallAfterDownload ? "Install Selected" : "Download Selected"))
            {
                Task.Run(() => programManager.InstallSelectedProgramsAsync());
            }
        }

        private void RenderProgressBar()
        {
            ImGui.ProgressBar(progress / 100f, new System.Numerics.Vector2(-1, 20), $"{progress}%");
        }

        private void RenderStatusBar()
        {
            ImGui.Text($"Status: {status}");
        }
    }

}