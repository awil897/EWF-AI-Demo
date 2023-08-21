// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StylesheetUi2.Views.Pages;
using StylesheetUi2.Views.Windows;

namespace StylesheetUi2.Services
{
    public class EventAggregator
    {
        public event Action<string, object> EventRaised;

        public void RaiseEvent(string eventName, object data)
        {
            EventRaised?.Invoke(eventName, data);
        }

        public void Subscribe(string eventName, Action<object> handler)
        {
            EventRaised += (name, data) =>
            {
                if (name == eventName)
                {
                    handler(data);
                }
            };
        }
    }
}
