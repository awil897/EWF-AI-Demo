// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Azure;
using Azure.AI.OpenAI;
using StylesheetUi2.Models;
using StylesheetUi2.Services;
using System.ComponentModel;
using System.Diagnostics;
using Wpf.Ui.Controls;


namespace StylesheetUi2.ViewModels.Pages
{
    public partial class DataViewModel : ObservableObject, INotifyPropertyChanged, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private string _apiKey = String.Empty;

        [ObservableProperty]
        private string _originalVariables = String.Empty;

        [ObservableProperty]
        private string _translatedVariables = String.Empty;

        [ObservableProperty]
        private string _explanationVariables = String.Empty;

        [ObservableProperty]
        private string _debugRawResponse = String.Empty;

        [ObservableProperty]
        private string _openAiResponse = String.Empty;

        [ObservableProperty]
        private string _selectedModel = String.Empty;

        [ObservableProperty]
        private bool _isMasterEnabled = Settings.Default.enabledMaster;

        [ObservableProperty]
        private string _markdownDocVariables = "";

        private string encryptedApiKey = Settings.Default.encryptedApiKey;

        public void OnNavigatedTo()
        {
            if (!_isInitialized)
                InitializeViewModel();
            Debug.WriteLine("DataViewModel- OnNavigatedTo");
            SelectedModel = Settings.Default.selectedModel;
            var eventAggregator = App.GetService<EventAggregator>();
            eventAggregator.Subscribe("EnabledMasterChanged", data =>
            {
                bool isEnabled = (bool)data;
                IsMasterEnabled = isEnabled;
            });
        }

        public void OnNavigatedFrom() 
        {
            Debug.WriteLine("DataViewModel- OnNavigatingFrom");
        }

        private void InitializeViewModel()
        {

            _isInitialized = true;
        }

        [RelayCommand]
        private async Task LoadApiKey()
        {
            await GetAiResponse();
        }

        private async Task GetAiResponse()
        {
            try
            { 
                OpenAiResponse = String.Empty;
                string authorizedCheck = Settings.Default.authorized;
                encryptedApiKey = Settings.Default.encryptedApiKey;
                SelectedModel = Settings.Default.selectedModel;
                MarkdownDocVariables = "";
                if (encryptedApiKey == null || encryptedApiKey == "" || authorizedCheck == "False")
                {
                    var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = "Error",
                        Content = "The API Key you entered does not appear to be in the correct format, try again.",
                    };
                    var results = await uiMessageBox.ShowDialogAsync();
                    return;
                }
                else
                {
                    encryptedApiKey = Settings.Default.encryptedApiKey;
                    ApiKey = ApiKeyUtility.Decrypt(encryptedApiKey);
                    string systemMessage = global::StylesheetUi2.Properties.Resources.system2;
                    string userMessage = global::StylesheetUi2.Properties.Resources.user2;
                    string chatResponse = global::StylesheetUi2.Properties.Resources.assistant2;
                    string sampleMessage = "I have the following variables:" + Environment.NewLine + OriginalVariables + Environment.NewLine + ExplanationVariables + Environment.NewLine + TranslatedVariables + Environment.NewLine + "What built-in VisualBasic method or methods would be needed to accomplish this?. Please give your response in formatted in markdown so that's it's very easy to read, and a short explanation of why the translation works. Explain like I may not understand VisualBasic or .NET that well.";


                    string nonAzureOpenAIApiKey = ApiKey;
                    var client = new OpenAIClient(nonAzureOpenAIApiKey, new OpenAIClientOptions());
                    var chatCompletionsOptions = new ChatCompletionsOptions()
                    {
                        Messages =
                        {
                            new ChatMessage(ChatRole.System, systemMessage),
                            new ChatMessage(ChatRole.User, userMessage),
                            new ChatMessage(ChatRole.Assistant, chatResponse),
                            new ChatMessage(ChatRole.User, sampleMessage),

                        }
                    };

                    Response<StreamingChatCompletions> response = await client.GetChatCompletionsStreamingAsync(
                        deploymentOrModelName: SelectedModel,
                        chatCompletionsOptions);

                    using StreamingChatCompletions streamingChatCompletions = response.Value;

                    await foreach (StreamingChatChoice choice in streamingChatCompletions.GetChoicesStreaming())
                    {
                        await foreach (ChatMessage message in choice.GetMessageStreaming())
                        {
                            OpenAiResponse = OpenAiResponse + message.Content;
                            MarkdownDocVariables = OpenAiResponse + message.Content;

                        }
                        OpenAiResponse = OpenAiResponse + Environment.NewLine;
                        MarkdownDocVariables = OpenAiResponse + Environment.NewLine;
                    }

                    //OpenAiResponse = response.Result.Value.;
                    return;
                }
            }
            catch (Exception ex) 
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Exception!",
                    Content = $"An error occurred: {ex.Message}",
                };
                var results = await uiMessageBox.ShowDialogAsync();
            }
        }

    }
}
