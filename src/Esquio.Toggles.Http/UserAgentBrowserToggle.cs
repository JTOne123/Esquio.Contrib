﻿using Esquio;
using Esquio.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Esquio.Toggles.Http
{
    [DesignType(Description = "Toggle that is active depending on request user agent browser information.", FriendlyName = "On Browser")]
    [DesignTypeParameter(ParameterName = Browsers, ParameterType = EsquioConstants.SEMICOLON_LIST_PARAMETER_TYPE, ParameterDescription = "Collection of browser names delimited by ';' character.")]
    public class UserAgentBrowserToggle
        : IToggle
    {
        private const string Browsers = nameof(Browsers);

        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ILogger<UserAgentBrowserToggle> _logger;
        private readonly IRuntimeFeatureStore _featureStore;

        public UserAgentBrowserToggle(IRuntimeFeatureStore featureStore, IHttpContextAccessor httpContextAccessor, ILogger<UserAgentBrowserToggle> logger)
        {
            _featureStore = featureStore ?? throw new ArgumentNullException(nameof(featureStore));
            _contextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task<bool> IsActiveAsync(string featureName, string productName = null, CancellationToken cancellationToken = default)
        {
            var feature = await _featureStore.FindFeatureAsync(featureName, productName, cancellationToken);
            var toggle = feature.GetToggle(typeof(UserAgentBrowserToggle).FullName);
            var data = toggle.GetData();

            var allowedBrowsers = data.Browsers.ToString();
            var currentBrowser = GetCurrentBrowser();

            if (allowedBrowsers != null && !String.IsNullOrEmpty(currentBrowser))
            {
                _logger.LogDebug($"{nameof(UserAgentBrowserToggle)} is trying to verify if {currentBrowser} is satisfying allowed browser configuration.");

                var tokenizer = new StringTokenizer(allowedBrowsers, EsquioConstants.DEFAULT_SPLIT_SEPARATOR);

                foreach (var segment in tokenizer)
                {
                    if (currentBrowser.IndexOf(segment.Value, StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        _logger.LogInformation($"The browser {currentBrowser} is satisfied using {allowedBrowsers} configuration.");

                        return true;
                    }
                    else
                    {
                        _logger.LogInformation($"The browser {currentBrowser} is not allowed with the parameter {segment.Value}.");
                    }
                }
            }

            return false;
        }

        private string GetCurrentBrowser()
        {
            const string UserAgent = "user-agent";

            return _contextAccessor.HttpContext
                .Request
                .Headers[UserAgent]
                .FirstOrDefault() ?? string.Empty;
        }
    }
}
