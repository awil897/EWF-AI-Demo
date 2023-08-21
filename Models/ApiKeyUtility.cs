// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Text;
using System.Security.Cryptography;

namespace StylesheetUi2.Models
{
    public class ApiKeyUtility
    {
        public static bool IsValidApiKeyFormat(string secretKey)
        {
            return Regex.IsMatch(secretKey, @"^sk-[a-zA-Z0-9]{32,}$");
        }

        public static async Task<bool> CheckApiKeyAuthorizationAsync(string secretKey)
        {
            using var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);

            var response = await httpClient.GetAsync("https://api.openai.com/v1/engines");
            return response.IsSuccessStatusCode;
        }

        public static string Encrypt(string plainText, string password = null)
        {
            var data = Encoding.Default.GetBytes(plainText);
            var pwd = !string.IsNullOrEmpty(password) ? Encoding.Default.GetBytes(password) : Array.Empty<byte>();
            var cipher = ProtectedData.Protect(data, pwd, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(cipher);
        }

        public static string Decrypt(string cipherText, string password = null)
        {
            var cipher = Convert.FromBase64String(cipherText);
            var pwd = !string.IsNullOrEmpty(password) ? Encoding.Default.GetBytes(password) : Array.Empty<byte>();
            var data = ProtectedData.Unprotect(cipher, pwd, DataProtectionScope.CurrentUser);
            return Encoding.Default.GetString(data);
        }
    }
}
