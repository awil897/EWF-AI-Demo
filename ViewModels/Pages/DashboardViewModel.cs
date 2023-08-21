// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Azure;
using Azure.AI.OpenAI;
using Newtonsoft.Json.Linq;
using RestSharp;
using StylesheetUi2.Models;
using StylesheetUi2.Services;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Navigation;
using System.Xml.Linq;
using Wpf.Ui.Contracts;
using Wpf.Ui.Controls;

namespace StylesheetUi2.ViewModels.Pages
{

    public partial class DashboardViewModel : ObservableObject, INotifyPropertyChanged, INavigationAware
    {
        [ObservableProperty]
        private int _counter = 0;

        [ObservableProperty]
        private string _apiKey = String.Empty;

        [ObservableProperty]
        private string _requestString = String.Empty;

        [ObservableProperty]
        private string _debugRawResponse = String.Empty;

        [ObservableProperty]
        private string _openAiResponse = String.Empty;

        [ObservableProperty]
        private string _selectedModel = String.Empty;

        [ObservableProperty]
        private bool _isMasterEnabled = Settings.Default.enabledMaster;

        [ObservableProperty]
        private string _markdownDoc = "";

        private string encryptedApiKey = Settings.Default.encryptedApiKey;

        public void OnNavigatedTo()
        {
            Debug.WriteLine("DashboardViewModel- OnNavigatedTo");
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
            Debug.WriteLine("DashboardViewModel- OnNavigatingFrom");
        }



        [RelayCommand]
        private async Task LoadApiKey()
        {
            await GetAiResponse();
        }

        private async Task GetAiResponse()
        {
            OpenAiResponse = String.Empty;
            string authorizedCheck = Settings.Default.authorized;
            encryptedApiKey = Settings.Default.encryptedApiKey;
            SelectedModel = Settings.Default.selectedModel;
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
                string systemMessage = global::StylesheetUi2.Properties.Resources.system;
                string userMessage = global::StylesheetUi2.Properties.Resources.user1;
                string chatResponse = global::StylesheetUi2.Properties.Resources.assistant1;
                string sampleMessage = RequestString + Environment.NewLine + "please give your response in formatted in markdown so that's it's very easy to read, and a short explanation of why the Stylesheet works. Explain like I may not understand XML that well." + Environment.NewLine + Environment.NewLine + DebugRawResponse;


                string nonAzureOpenAIApiKey = ApiKey;
                var client = new OpenAIClient(nonAzureOpenAIApiKey, new OpenAIClientOptions());
                var chatCompletionsOptions = new ChatCompletionsOptions()
                {
                    Messages =
                    {
                        new ChatMessage(ChatRole.System, systemMessage),
                        new ChatMessage(ChatRole.User, userMessage),
                        new ChatMessage(ChatRole.Assistant, "Sure, I can help with that. Do you know the names of the nodes you want to assign to variables?"),
                        new ChatMessage(ChatRole.User, "Yep. I need to assign all the ElemCanocValDesc node values that have a sibling ElemCanocVal node value of Index to a variable named SynergyIndexes."),
                        new ChatMessage(ChatRole.Assistant, "I can help with that. Do you need any other variables assigned from this reponse?"),
                        new ChatMessage(ChatRole.User, "No, that's it."),
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
                        MarkdownDoc = OpenAiResponse + message.Content;

                    }
                    OpenAiResponse = OpenAiResponse + Environment.NewLine;
                    MarkdownDoc = OpenAiResponse + Environment.NewLine;
                }

                //OpenAiResponse = response.Result.Value.;
                return;
            }
        }

        [RelayCommand]
        private void OnCounterIncrement()
        {
            Counter++;
        }
    }
}
