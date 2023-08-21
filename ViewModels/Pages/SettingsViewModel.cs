// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Newtonsoft.Json.Linq;
using RestSharp;
using System.Net;
using Wpf.Ui.Controls;
using StylesheetUi2.Models;
using StylesheetUi2.Services;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace StylesheetUi2.ViewModels.Pages
{
    public partial class SettingsViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;
        private string encryptedApiKey = Settings.Default.encryptedApiKey;

        [ObservableProperty]
        private string _appVersion = String.Empty;

        [ObservableProperty]
        private string _apiKey = String.Empty;

        [ObservableProperty]
        private IList<string> _availableModels = new ObservableCollection<string>();

        [ObservableProperty]
        private string _currentItem = String.Empty;

        [ObservableProperty]
        private Wpf.Ui.Appearance.ThemeType _currentTheme = Wpf.Ui.Appearance.ThemeType.Unknown;

        public void OnNavigatedTo()
        {
            if (!_isInitialized)
                InitializeViewModel();
            LoadApiKey();
            LoadModelList();
            CurrentItem = Settings.Default.selectedModel;
            Debug.WriteLine("SettingsViewModel- OnNavigatedTo");
            Debug.WriteLine(CurrentItem);
        }

        public void OnNavigatedFrom() 
        {

            LoadApiKey();
            LoadModelList();
            Settings.Default.selectedModel = CurrentItem;
            Settings.Default.Save();
            Debug.WriteLine("SettingsViewModel- OnNavigatedFrom");
            Debug.WriteLine(CurrentItem);
        }


       
        // Retrieve the saved API key from settings when ViewModel is initialized
        // Define the path to the API key file within the user's local application data folder


        // Save the API key to settings when it changes

        [RelayCommand]
        public void SubmitApiKey()
        {
            SaveApiKey();
        }

        [RelayCommand]
        private void LoadApiKey()
        {
            string authorizedCheck = Settings.Default.authorized;
            encryptedApiKey = Settings.Default.encryptedApiKey;
            if (encryptedApiKey == null || encryptedApiKey == "" || authorizedCheck == "False")
            {
                ApiKey = String.Empty;
            }
            else
            {
                encryptedApiKey = Settings.Default.encryptedApiKey;
                ApiKey = ApiKeyUtility.Decrypt(encryptedApiKey);
            }
        }

        public bool IsMasterEnabled
        {
            get { return Settings.Default.enabledMaster; }
            set
            {
                Settings.Default.enabledMaster = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(IsMasterEnabled)); // Notify of the property change
                var eventAggregator = App.GetService<EventAggregator>();
                eventAggregator.RaiseEvent("EnabledMasterChanged", true);
            }
        }

        [RelayCommand]
        public void LoadModelList()
        {
            AvailableModels = Settings.Default.availableModels;
        }

        // Save the API key to Properties.Settings.Default
        private async void SaveApiKey()
        {
            string url = "https://api.openai.com/v1/models";
            bool isAuthorized = false;
            bool isValidFormat = false;
            Debug.WriteLine("SettingsViewModel - Saving API Key");
            isValidFormat = ApiKeyUtility.IsValidApiKeyFormat(_apiKey);
            Settings.Default.validFormat = isValidFormat.ToString();
            Settings.Default.Save();

            if (isValidFormat == true)
            {
                isAuthorized = await ApiKeyUtility.CheckApiKeyAuthorizationAsync(_apiKey);
                Settings.Default.authorized = isAuthorized.ToString();
                Settings.Default.Save();
            }
            else
            {
                Settings.Default.encryptedApiKey = "";
                Settings.Default.Save();
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Error",
                    Content = "The API Key you entered does not appear to be in the correct format, try again.",
                };
                var result = await uiMessageBox.ShowDialogAsync();
                return;
            }

            if (isAuthorized == true) 
            {
                encryptedApiKey = ApiKeyUtility.Encrypt(ApiKey);
                Settings.Default.encryptedApiKey = encryptedApiKey;
                Settings.Default.Save();

                try
                {
                    var client = new RestClient(url);
                    var request = new RestRequest();
                    request.AddHeader("Authorization", "Bearer " + ApiKey);
                    var response = client.Execute(request);

                    // Check if the request was successful
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                        {
                            Title = "Error",
                            Content = $"Request failed with status code: {response.StatusCode}",
                        };
                        var results = await uiMessageBox.ShowDialogAsync();
                        return;
                    }

                    JObject parsed = JObject.Parse(response.Content);
                    string result = ((JProperty)parsed.First).Name;
                    Settings.Default.webResponse = result;
                    Settings.Default.Save();

                    // Check if result is "object" or "error"
                    if (result == "error")
                    {

                        throw new Exception("'Error' received from OpenAI, check your API-Key");
                    }

                    if (result == "object")
                    {
                        try
                        {
                            JArray myArray = (JArray)parsed["data"];
                            dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                            List<string> modelList = new List<string>();
                            List<string> compatibleList = new List<string>
                                { "gpt-4", "gpt-4-0613", "gpt-4-32k", "gpt-4-32k-0613", "gpt-3.5-turbo", "gpt-3.5-turbo-0613", "gpt-3.5-turbo-16k", "gpt-3.5-turbo-16k-0613" };

                            foreach (JObject item in myArray)
                            {
                                string modelNames = (string)item["id"];
                                bool b = compatibleList.Any(s => modelNames.Contains(s));
                                if (b == true)
                                {
                                    modelList.Add(modelNames);
                                }
                            }
                            Settings.Default.availableModels = modelList;
                            Settings.Default.Save();
                            AvailableModels = Settings.Default.availableModels;
                            IsMasterEnabled = true;
                            Debug.WriteLine(CurrentItem);
                            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                            {
                                Title = "Success!",
                                Content = "The API Key you entered was authenticated and successfully saved. Select a model to test with.",
                            };
                            var results = await uiMessageBox.ShowDialogAsync();
                        }
                        catch (Exception ex)
                        {
                            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                            {
                                Title = "Unhandled Exception",
                                Content = $"An error occurred: {ex.Message}",
                            };
                            var results = await uiMessageBox.ShowDialogAsync();
                        }
                    }

                }
                catch (Exception ex)
                {
                    var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = "Unhandled Exception",
                        Content = $"An error occurred: {ex.Message}",
                    };
                    var results = await uiMessageBox.ShowDialogAsync();
                }
            }
            else 
            {
                Settings.Default.encryptedApiKey = "";
                Settings.Default.Save();
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Error",
                    Content = "The API Key you entered could not be authorized with OpenAI, please try again",
                };
                var result = await uiMessageBox.ShowDialogAsync();
                return;
            }
        }

       

        private void InitializeViewModel()
        {
            CurrentTheme = Wpf.Ui.Appearance.Theme.GetAppTheme();
            AppVersion = $"EWF - AI Tools Demo - {GetAssemblyVersion()}";

            _isInitialized = true;
        }

        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }

        [RelayCommand]
        private void OnChangeTheme(string parameter)
        {
            switch (parameter)
            {
                case "theme_light":
                    if (CurrentTheme == Wpf.Ui.Appearance.ThemeType.Light)
                        break;

                    Wpf.Ui.Appearance.Theme.Apply(Wpf.Ui.Appearance.ThemeType.Light);
                    CurrentTheme = Wpf.Ui.Appearance.ThemeType.Light;

                    break;

                default:
                    if (CurrentTheme == Wpf.Ui.Appearance.ThemeType.Dark)
                        break;

                    Wpf.Ui.Appearance.Theme.Apply(Wpf.Ui.Appearance.ThemeType.Dark);
                    CurrentTheme = Wpf.Ui.Appearance.ThemeType.Dark;

                    break;
            }
        }
    }
}
