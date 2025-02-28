﻿using Community.VisualStudio.Toolkit;using EnvDTE;using Microsoft.VisualStudio.Shell;using System;
using System.Collections.Generic;using System.Linq;using System.Windows;using System.Windows.Controls;using System.Windows.Media;using System.Windows.Media.Imaging;
using CheckBox = System.Windows.Controls.CheckBox;using Path = System.IO.Path;
using Project = EnvDTE.Project;using Solution = EnvDTE.Solution;using UserControl = System.Windows.Controls.UserControl;namespace JeffPires.VisualChatGPTStudio.ToolWindows{
    /// <summary>
    /// Represents a user control for the Terminal Window Solution Context.
    /// </summary>
    public partial class TerminalWindowSolutionContextControl : UserControl    {
        #region Properties
        private DTE dte;        private SolidColorBrush foreGroundColor;
        private readonly List<string> validProjectTypes;
        private readonly List<string> validProjectItems;
        private readonly List<string> invalidProjectItems;

        #endregion Properties
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the TerminalWindowSolutionContextControl class.
        /// </summary>
        public TerminalWindowSolutionContextControl()        {            this.InitializeComponent();

            validProjectTypes =
            [
                ".csproj",
                ".vbproj",
                ".vcxproj",
                ".fsproj",
                ".pyproj",
                ".jsproj",
                ".sqlproj",
                ".wixproj",
                ".njsproj",
                ".shproj"
            ];

            validProjectItems =
            [
                ".config",
                ".cs",
                ".css",
                ".html",
                ".js",
                ".json",
                ".md",
                ".sql",
                ".ts",
                ".vb",
                ".xml",
                ".xaml"
            ];

            invalidProjectItems =
            [
                ".png",
                ".bmp",
                ".exe",
                ".dll",
                ".suo",
                ".vsct",
                ".vssscc",
                ".vspscc",
                ".user",
                ".vsixmanifest",
                ".pdb"
            ];        }

        #endregion Constructors
        #region Event Handlers

        /// <summary>
        /// Event handler for the btnRefresh button click event. 
        /// Retrieves the current text color from the application resources and sets it as the foreground color for the button. 
        /// Calls the PopulateTreeViewAsync method to populate the tree view asynchronously.
        /// </summary>
        private void btnRefresh_Click(object sender, RoutedEventArgs e)        {            Color textColor = ((SolidColorBrush)Application.Current.Resources[VsBrushes.WindowTextKey]).Color;            foreGroundColor = new SolidColorBrush(textColor);            txtFilter.Text = string.Empty;            _ = PopulateTreeViewAsync();        }

        /// <summary>
        /// Handles the TextChanged event for the txtFilter control. 
        /// It retrieves the current text from the filter input, converts it to lowercase, 
        /// and applies the filter to the items in the tree view.
        /// </summary>
        private void txtFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filterText = txtFilter.Text.ToLower();

            FilterTreeViewItems(treeView.Items, filterText);

            ExpandOrCollapseAllItems(treeView.Items, !string.IsNullOrWhiteSpace(filterText));
        }

        #endregion Event Handlers
        #region Methods

        /// <summary>
        /// Populates the TreeView asynchronously with the solution and project items.
        /// </summary>
        private async System.Threading.Tasks.Task PopulateTreeViewAsync()        {            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();            dte ??= await VS.GetServiceAsync<DTE, DTE>();            Solution solution = dte.Solution;            string solutionName = Path.GetFileName(solution.FullName);            if (string.IsNullOrWhiteSpace(solutionName))            {                return;            }            TreeViewItem solutionNode;            if (treeView.Items.Count > 0)            {                treeView.Items.Clear();            }            solutionNode = SetupTreeViewItem(solutionName);            treeView.Items.Add(solutionNode);            foreach (Project project in solution.Projects)            {                if (string.IsNullOrWhiteSpace(project?.Name))
                {
                    continue;
                }                TreeViewItem projectNode = SetupTreeViewItem(project.Name);                solutionNode.Items.Add(projectNode);                PopulateProjectItems(project.ProjectItems, projectNode);            }        }

        /// <summary>
        /// Populates the project items in a tree view.
        /// </summary>
        /// <param name="items">The project items to populate.</param>
        /// <param name="parentNode">The parent node in the tree view.</param>
        private void PopulateProjectItems(ProjectItems items, TreeViewItem parentNode)        {            if (items == null)            {                return;            }            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (ProjectItem item in items)            {                if (!item.Kind.Equals(Constants.vsProjectItemKindSolutionItems) &&
                    !item.Kind.Equals(Constants.vsProjectItemKindPhysicalFile) &&
                    !item.Kind.Equals(Constants.vsProjectItemKindPhysicalFolder) &&
                    !item.Kind.Equals(Constants.vsProjectItemKindVirtualFolder))
                {
                    continue;
                }                if ((item.Kind.Equals(Constants.vsProjectItemKindPhysicalFolder) || item.Kind.Equals(Constants.vsProjectItemKindVirtualFolder)) &&
                    (item.ProjectItems == null || item.ProjectItems.Count == 0))
                {
                    continue;
                }

                if (invalidProjectItems.Any(i => item.Name.EndsWith(i)))
                {
                    continue;
                }                TreeViewItem itemNode = SetupTreeViewItem(item.Name);

                parentNode.Items.Add(itemNode);

                PopulateProjectItems(item.ProjectItems, itemNode);
                PopulateProjectItems(item.SubProject?.ProjectItems, itemNode);
            }        }

        /// <summary>
        /// Recursively checks or unchecks all child items of a TreeViewItem based on the provided isChecked value.
        /// </summary>
        /// <param name="parentItem">The parent TreeViewItem.</param>
        /// <param name="isChecked">The value indicating whether the child items should be checked or unchecked.</param>
        private void CheckChildItems(TreeViewItem parentItem, bool isChecked)        {            foreach (object item in parentItem.Items)            {                if (item is TreeViewItem treeViewItem)                {                    CheckBox checkbox = FindCheckBox(treeViewItem);                    checkbox.IsChecked = isChecked;                    CheckChildItems(treeViewItem, isChecked);                }            }        }

        /// <summary>
        /// Sets up a TreeViewItem with a CheckBox as its header.
        /// </summary>
        /// <param name="name">The name to be displayed in the CheckBox.</param>
        /// <returns>The configured TreeViewItem.</returns>
        private TreeViewItem SetupTreeViewItem(string name)        {            TreeViewItem itemNode = new();            StackPanel stackPanel = new()
            {
                Orientation = Orientation.Horizontal
            };            Image iconImage = new()
            {
                Source = GetFileIcon(name),
                Width = 20,
                Height = 20
            };

            stackPanel.Children.Add(iconImage);            CheckBox checkBox = new()            {                Content = name,                IsChecked = false,                Foreground = foreGroundColor,                FontSize = 15,                Margin = new Thickness(5, 5, 0, 0)            };            stackPanel.Children.Add(checkBox);

            itemNode.Header = stackPanel;            itemNode.IsExpanded = false;            checkBox.Checked += (sender, e) =>            {                CheckChildItems(itemNode, true);            };            checkBox.Unchecked += (sender, e) =>            {                CheckChildItems(itemNode, false);            };            return itemNode;        }

        /// <summary>
        /// Retrieves the icon image source for a given file based on its extension.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <returns>The image source of the file icon.</returns>
        public ImageSource GetFileIcon(string fileName)
        {
            BitmapImage imageSource = new();
            string fileExtension = string.Empty;

            try
            {
                fileExtension = Path.GetExtension(fileName);
            }
            catch (Exception)
            {

            }

            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                fileExtension = "folder";
            }
            else if (fileExtension == ".sln")
            {
                fileExtension = "sln";
            }
            else if (validProjectTypes.Any(i => i == fileExtension))
            {
                fileExtension = "vs";
            }
            else if (!validProjectItems.Any(i => i == fileExtension))
            {
                return imageSource;
            }

            string uriSource = $"pack://application:,,,/VisualChatGPTStudio;component/Resources/FileTypes/{fileExtension.Replace(".", string.Empty)}.png";

            imageSource.BeginInit();
            imageSource.UriSource = new Uri(uriSource);
            imageSource.EndInit();

            return imageSource;
        }

        /// <summary>
        /// Retrieves the names of the selected files in a tree view.
        /// </summary>
        /// <returns>
        /// A list of strings containing the names of the selected files.
        /// </returns>
        public List<string> GetSelectedFilesName()        {            if (treeView.Items.Count == 0)
            {
                return [];
            }            return GetSelectedFilesName((TreeViewItem)treeView.Items.GetItemAt(0));        }

        /// <summary>
        /// Retrieves the names of selected files from a TreeViewItem.
        /// </summary>
        /// <param name="root">The root TreeViewItem.</param>
        /// <returns>A list of selected file names.</returns>
        private List<string> GetSelectedFilesName(TreeViewItem root)        {            List<string> selectedFilesName = [];            foreach (object item in root.Items)            {                if (item is TreeViewItem treeViewItem)                {                    CheckBox checkBox = FindCheckBox(treeViewItem);                    if (checkBox != null && checkBox.IsChecked == true)                    {                        selectedFilesName.Add(checkBox.Content.ToString());                    }                    selectedFilesName.AddRange(GetSelectedFilesName(treeViewItem));                }            }            return selectedFilesName;        }

        /// <summary>
        /// Finds and returns the CheckBox control within a TreeViewItem.
        /// </summary>
        /// <param name="item">The TreeViewItem to search within.</param>
        /// <returns>The CheckBox control if found, otherwise null.</returns>
        private CheckBox FindCheckBox(TreeViewItem item)        {            if (item.Header is StackPanel stackPanel)            {                if (stackPanel.Children?[1] is CheckBox checkbox)
                {
                    return checkbox;
                }
            }            foreach (object subItem in item.Items)            {                if (subItem is TreeViewItem subTreeViewItem)                {                    CheckBox subCheckBox = FindCheckBox(subTreeViewItem);                    if (subCheckBox != null)                    {                        return subCheckBox;                    }                }            }            return null;        }

        /// <summary>
        /// Filters the items in a TreeView based on the specified filter text.
        /// Sets the visibility of each TreeViewItem to either Visible or Collapsed
        /// depending on whether it matches the filter criteria.
        /// </summary>
        /// <param name="items">The collection of TreeViewItems to filter.</param>
        /// <param name="filterText">The text used to filter the TreeViewItems.</param>
        private void FilterTreeViewItems(ItemCollection items, string filterText)
        {
            foreach (TreeViewItem item in items)
            {
                bool isVisible = IsItemVisible(item, filterText);
                item.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

                FilterTreeViewItems(item.Items, filterText);
            }
        }

        /// <summary>
        /// Determines whether a given TreeViewItem is visible based on the specified filter text.
        /// It checks if the item's CheckBox content contains the filter text, and recursively checks its child items.
        /// </summary>
        /// <param name="item">The TreeViewItem to check for visibility.</param>
        /// <param name="filterText">The text used to filter the visibility of the item.</param>
        /// <returns>True if the item or any of its child items are visible based on the filter text; otherwise, false.</returns>
        private bool IsItemVisible(TreeViewItem item, string filterText)
        {
            CheckBox checkBox = FindCheckBox(item);

            if (checkBox != null && checkBox.Content.ToString().ToLower().Contains(filterText))
            {
                return true;
            }

            foreach (TreeViewItem child in item.Items)
            {
                if (IsItemVisible(child, filterText))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Expands or collapses all items in the specified item collection based on the provided boolean value.
        /// </summary>
        /// <param name="items">The collection of items (TreeViewItems) to expand or collapse.</param>
        /// <param name="isExpanded">A boolean value indicating whether to expand (true) or collapse (false) the items.</param>
        private void ExpandOrCollapseAllItems(ItemCollection items, bool isExpanded)
        {
            foreach (TreeViewItem item in items)
            {
                item.IsExpanded = isExpanded;
                ExpandOrCollapseAllItems(item.Items, isExpanded);
            }
        }

        #endregion Methods  
    }}