using ClickableTransparentOverlay;
using ImGuiNET;
using System.Runtime.InteropServices;

namespace QuickInstall
{
    class UIManager : Overlay
    {
        private ProgramManager programManager;
        private int progress = 0;
        private string status = "Idle";
        private string searchQuery = "";

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

            ImGui.Spacing();
            ImGui.Text("Configuration:");
            ImGui.Separator();
            RenderArchitectureSelection();
            RenderInstallAfter();
            RenderTagFilter();

            ImGui.Spacing();
            RenderSearchBar(); // Добавляем строку поиска
            ImGui.Text("Programs:");
            ImGui.Separator();
            RenderProgramList();

            ImGui.Spacing();
            RenderInstallButton();

            ImGui.Spacing();
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
            bool isSearchActive = !string.IsNullOrEmpty(searchQuery);

            if (!isSearchActive)
            {
                bool allSelected = programManager.Programs
                    .Where(p => programManager.SelectedTag == "all" || p.Tags.Contains(programManager.SelectedTag.ToLower()))
                    .All(p => p.IsSelected);

                bool noneSelected = programManager.Programs
                    .Where(p => programManager.SelectedTag == "all" || p.Tags.Contains(programManager.SelectedTag.ToLower()))
                    .All(p => !p.IsSelected);

                bool selectAll = allSelected && !noneSelected;

                if (ImGui.Checkbox($"Select All in {programManager.SelectedTag.ToUpper()}", ref selectAll))
                {
                    foreach (var program in programManager.Programs)
                    {
                        if (programManager.SelectedTag == "all" || program.Tags.Contains(programManager.SelectedTag.ToLower()))
                        {
                            program.IsSelected = selectAll;
                        }
                    }
                }

                ImGui.Separator();
            }

            ImGui.BeginChild("ProgramList");
            foreach (var program in programManager.Programs)
            {
                if ((programManager.SelectedTag == "all" || program.Tags.Contains(programManager.SelectedTag.ToLower())) &&
                    (string.IsNullOrEmpty(searchQuery) || program.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)))
                {
                    bool isSelected = program.IsSelected;
                    if (ImGui.Checkbox(program.Name, ref isSelected))
                    {
                        program.IsSelected = isSelected;
                    }
                }
            }
            ImGui.EndChild();

            ImGui.Separator();
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
            ImGui.Separator();
            ImGui.Text("Status:");
            ImGui.TextWrapped(status);
        }

        private void RenderSearchBar()
        {
            ImGui.Text("Search:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            ImGui.InputText("##search", ref searchQuery, 256);

            if (!string.IsNullOrEmpty(searchQuery))
            {
                ImGui.SameLine();
                if (ImGui.Button("Clear"))
                {
                    searchQuery = "";
                }
            }
        }
    }
}
