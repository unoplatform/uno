using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace SamplesApp.Samples.Windows_Storage_Pickers
{
    public sealed partial class FileOpenPickerTests : Page
    {
        private StorageFile _selectedFile;
        private string _originalContent;
        private const string ModificationSuffix = "\n\n[Modified by Uno Platform Test at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]";

        public FileOpenPickerTests()
        {
            this.InitializeComponent();
        }

        private async void OnSelectFileClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Reset UI
                ResetUI();

                // Create and configure the file picker
                var picker = new FileOpenPicker
                {
                    ViewMode = PickerViewMode.Thumbnail,
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary
                };

                // Add file type filters
                picker.FileTypeFilter.Add(".txt");
                picker.FileTypeFilter.Add(".json");
                picker.FileTypeFilter.Add(".xml");
                picker.FileTypeFilter.Add("*");

                // Show the file picker and get the selected file
                _selectedFile = await picker.PickSingleFileAsync();

                if (_selectedFile != null)
                {
                    // Update UI
                    FileNameText.Text = $"Selected file: {_selectedFile.Path}";
                    
                    try
                    {
                        // Read the file content
                        _originalContent = await FileIO.ReadTextAsync(_selectedFile);
                        FileContentText.Text = _originalContent;
                        
                        // Enable modification button
                        ModifyFileButton.IsEnabled = true;
                        ModificationStatusText.Text = "File loaded successfully. You can now modify it.";
                        ModificationStatusText.Foreground = new SolidColorBrush(Windows.UI.Colors.Green);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        ShowError("Cannot read the file. Please select a different file.");
                    }
                    catch (Exception ex)
                    {
                        ShowError($"Error reading file: {ex.Message}");
                    }
                }
                else
                {
                    ResetUI();
                    ModificationStatusText.Text = "File selection was cancelled.";
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error: {ex.Message}");
            }
        }

        private async void OnModifyFileClick(object sender, RoutedEventArgs e)
        {
            if (_selectedFile == null) return;

            try
            {
                // Create modified content
                string modifiedContent = _originalContent + ModificationSuffix;
                
                // Write the modified content back to the file
                await FileIO.WriteTextAsync(_selectedFile, modifiedContent);
                
                // Update UI
                FileContentText.Text = modifiedContent;
                ModificationStatusText.Text = "File has been successfully modified. Please verify the changes in the source location.";
                ModificationStatusText.Foreground = new SolidColorBrush(Windows.UI.Colors.Green);
                
                // Show verification instructions
                VerificationInstructions.Visibility = Visibility.Visible;
                
                // Disable modify button to prevent multiple modifications
                ModifyFileButton.IsEnabled = false;
            }
            catch (UnauthorizedAccessException)
            {
                ShowError("Cannot modify the file. The file might be read-only or you don't have sufficient permissions.");
            }
            catch (Exception ex)
            {
                ShowError($"Error modifying file: {ex.Message}");
            }
        }

        private void ResetUI()
        {
            _selectedFile = null;
            _originalContent = string.Empty;
            FileContentText.Text = string.Empty;
            ModifyFileButton.IsEnabled = false;
            VerificationInstructions.Visibility = Visibility.Collapsed;
        }

        private void ShowError(string message)
        {
            ModificationStatusText.Text = message;
            ModificationStatusText.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
        }
    }
}
