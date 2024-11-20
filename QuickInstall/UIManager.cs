using ClickableTransparentOverlay;
using ImGuiNET;
using System.Runtime.InteropServices;

namespace QuickInstall
{
    public class UIManager : Overlay
    {
        private readonly ProgramManager _programManager;
        private int _progress;
        private string _status = "Idle";
        private string _searchQuery = string.Empty;
        private bool _isFirstRender = true;

        public UIManager(ProgramManager programManager) : base(GetScreenWidth(), GetScreenHeight())
        {
            _programManager = programManager ?? throw new ArgumentNullException(nameof(programManager));
            _programManager.ProgressChanged += p => _progress = p;
            _programManager.StatusChanged += s => _status = s;
        }

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);
       
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static int GetScreenWidth() => GetSystemMetrics(0);
        public static int GetScreenHeight() => GetSystemMetrics(1);

        protected override void Render()
        {
            if (_isFirstRender)
            {
                ImGui.SetNextWindowPos(new System.Numerics.Vector2(GetScreenWidth() / 2, GetScreenHeight() / 2),
                    ImGuiCond.Always, new System.Numerics.Vector2(0.5f, 0.5f));
                _isFirstRender = false;
            }

            ImGui.SetNextWindowSizeConstraints(new System.Numerics.Vector2(850, 429), new System.Numerics.Vector2(float.MaxValue, float.MaxValue));
            ImGui.Begin("QuickInstall", ImGuiWindowFlags.MenuBar);

            RenderMenuBar();

            ImGui.BeginChild("MainContent", new System.Numerics.Vector2(0, -ImGui.GetFrameHeightWithSpacing()));
            ImGui.Columns(2, "MainColumns");

            // Left panel
            ImGui.BeginChild("LeftPanel", new System.Numerics.Vector2(0, 0));
            ImGui.Text("Configuration");
            ImGui.Separator();
            RenderArchitectureSelection();
            RenderInstallAfterOption();
            ImGui.Separator();
            RenderSearchBar();
            ImGui.Separator();
            RenderTags();
            ImGui.EndChild();

            ImGui.NextColumn();

            // Right panel
            ImGui.BeginChild("RightPanel", new System.Numerics.Vector2(0, 0));
            RenderProgramList();
            ImGui.EndChild();

            ImGui.Columns(1);
            ImGui.EndChild();

            ImGui.End();
        }

        private void RenderMenuBar()
        {
            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("Menu"))
                {
                    if (ImGui.MenuItem("Minimize Window"))
                    {
                        MinimizeWindow();
                    }

                    if (ImGui.MenuItem("Exit"))
                    {
                        Environment.Exit(0);
                    }

                    ImGui.EndMenu();
                }

                RenderInstallButton();
                RenderProgressBar();
                ImGui.EndMenuBar();
            }
        }

        private void MinimizeWindow()
        {
            const int SW_MINIMIZE = 6;
            IntPtr hwnd = GetActiveWindow();
            ShowWindow(hwnd, SW_MINIMIZE);
        }

        private void RenderInstallButton()
        {
            if (ImGui.Button(_programManager.IsInstallAfterDownload ? "Install Selected" : "Download Selected"))
            {
                Task.Run(() => _programManager.InstallSelectedProgramsAsync());
            }
        }

        private void RenderProgressBar()
        {
            ImGui.SameLine();
            ImGui.ProgressBar(_progress / 100f, new System.Numerics.Vector2(200, 20), $"{_progress}%");
            ImGui.SameLine();
            ImGui.TextColored(GetStatusColor(_status), _status);
        }

        private System.Numerics.Vector4 GetStatusColor(string status)
        {
            if (status.Contains("Idle", StringComparison.OrdinalIgnoreCase))
                return new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1.0f); // Grey
            else if (status.Contains("Downloading", StringComparison.OrdinalIgnoreCase))
                return new System.Numerics.Vector4(0.0f, 0.5f, 1.0f, 1.0f); // Blue
            else if (status.Contains("Installing", StringComparison.OrdinalIgnoreCase))
                return new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1.0f); // Green
            else if (status.Contains("Error", StringComparison.OrdinalIgnoreCase))
                return new System.Numerics.Vector4(1.0f, 0.0f, 0.0f, 1.0f); // Red
            else
                return new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1.0f); // White
        }

        private void RenderTags()
        {
            ImGui.Text("Tags:");
            ImGui.BeginChild("TagFilter", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 200));

            foreach (var tag in _programManager.Tags)
            {
                if (tag == _programManager.SelectedTag)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.0f, 0.8f, 0.0f, 1.0f)); // Green
                }
                if (ImGui.Selectable(tag.ToUpper(), tag == _programManager.SelectedTag))
                {
                    _programManager.SelectedTag = tag;
                }
                if (tag == _programManager.SelectedTag)
                {
                    ImGui.PopStyleColor();
                }
            }

            ImGui.EndChild();
        }

        private void RenderSearchBar()
        {
            ImGui.Text("Search Programs:");
            ImGui.SetNextItemWidth(150);
            ImGui.InputText("##search", ref _searchQuery, 256);

            if (!string.IsNullOrEmpty(_searchQuery))
            {
                ImGui.SameLine();
                if (ImGui.Button("Clear"))
                {
                    _searchQuery = string.Empty;
                }
            }
        }

        private void RenderProgramList()
        {
            bool allSelected = _programManager.Programs
                .Where(p => _programManager.SelectedTag == "all" || p.Tags.Contains(_programManager.SelectedTag.ToLower()))
                .All(p => p.IsSelected);

            bool selectAll = allSelected;

            if (ImGui.Checkbox($"Select All in {_programManager.SelectedTag.ToUpper()}", ref selectAll))
            {
                foreach (var program in _programManager.Programs)
                {
                    if (_programManager.SelectedTag == "all" || program.Tags.Contains(_programManager.SelectedTag.ToLower()))
                    {
                        program.IsSelected = selectAll;
                    }
                }
            }

            ImGui.Separator();

            foreach (var program in _programManager.Programs)
            {
                if ((_programManager.SelectedTag == "all" || program.Tags.Contains(_programManager.SelectedTag.ToLower())) &&
                    (string.IsNullOrEmpty(_searchQuery) || program.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase)))
                {
                    bool isSelected = program.IsSelected;
                    if (ImGui.Checkbox(program.Name, ref isSelected))
                    {
                        program.IsSelected = isSelected;
                    }
                }
            }
        }

        private void RenderArchitectureSelection()
        {
            ImGui.Text("Select Architecture:");
            if (ImGui.RadioButton("32-bit", _programManager.SelectedArchitecture == "32-bit"))
            {
                _programManager.SelectedArchitecture = "32-bit";
            }
            ImGui.SameLine();
            if (ImGui.RadioButton("64-bit", _programManager.SelectedArchitecture == "64-bit"))
            {
                _programManager.SelectedArchitecture = "64-bit";
            }
        }

        private void RenderInstallAfterOption()
        {
            bool installAfterDownload = _programManager.IsInstallAfterDownload;
            ImGui.Text("Install after download");
            ImGui.SameLine();
            if (ImGui.Checkbox("##InstallAfterDownload", ref installAfterDownload))
            {
                _programManager.IsInstallAfterDownload = installAfterDownload;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Automatically install programs after downloading them.");
            }
        }
    }
}
