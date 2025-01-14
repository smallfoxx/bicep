// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Azure.Deployments.Core.Extensions;
using Bicep.Core.Analyzers.Interfaces;
using Bicep.Core.Configuration;
using Bicep.Core.Diagnostics;
using Bicep.Core.Parsing;
using Bicep.Core.Semantics;

namespace Bicep.Core.Analyzers.Linter
{
    public class LinterAnalyzer : IBicepAnalyzer
    {
        public const string SettingsRoot = "analyzers";
        public const string AnalyzerName = "core";
        public static string LinterEnabledSetting => $"{SettingsRoot}:{AnalyzerName}:enabled";
        public static string LinterVerboseSetting => $"{SettingsRoot}:{AnalyzerName}:verbose";
        private ConfigHelper configHelper;
        private ImmutableArray<IBicepAnalyzerRule> RuleSet;
        private ImmutableArray<IDiagnostic> RuleCreationErrors;

        // TODO: This should be controlled by a core component, not an analyzer
        public const string FailedRuleCode = "linter-internal-error";

        public LinterAnalyzer(ConfigHelper configHelper)
        {
            this.configHelper = configHelper;
            (RuleSet, RuleCreationErrors) = CreateLinterRules();
        }

        private bool LinterEnabled => this.configHelper.GetValue(LinterEnabledSetting, true);
        private bool LinterVerbose => this.configHelper.GetValue(LinterVerboseSetting, true);

        private (ImmutableArray<IBicepAnalyzerRule> rules, ImmutableArray<IDiagnostic> errors) CreateLinterRules()
        {
            List<IDiagnostic> errors = new List<IDiagnostic>();
            List<IBicepAnalyzerRule> rules = new List<IBicepAnalyzerRule>();

            var ruleTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(IBicepAnalyzerRule).IsAssignableFrom(t)
                            && t.IsClass
                            && t.IsPublic
                            && t.GetConstructor(Type.EmptyTypes) != null);

            foreach (var ruleType in ruleTypes)
            {
                try
                {
                    rules.Add((IBicepAnalyzerRule)Activator.CreateInstance(ruleType));
                }
                catch (Exception ex)
                {
                    string message = string.Format("Analyzer '{0}' could not instantiate rule '{1}'. {2}",
                        AnalyzerName,
                        ruleType.Name,
                        ex.InnerException?.Message ?? ex.Message);
                    errors.Add(CreateInternalErrorDiagnostic(AnalyzerName, message));
                }
            }

            return (rules.ToImmutableArray(), errors.ToImmutableArray());
        }

        public IEnumerable<IBicepAnalyzerRule> GetRuleSet() => RuleSet;

        public IEnumerable<IDiagnostic> Analyze(SemanticModel semanticModel)
        {
            var diagnostics = new List<IDiagnostic>();

            this.RuleSet.ForEach(r => r.Configure(this.configHelper.Config));

            if (this.LinterEnabled)
            {
                // Add diaagnostics for rules that failed to load
                diagnostics.AddRange(RuleCreationErrors);

                // add an info diagnostic for local configuration reporting
                if (this.LinterVerbose)
                {
                    diagnostics.Add(GetConfigurationDiagnostic());
                }

                diagnostics.AddRange(RuleSet.Where(rule => rule.IsEnabled())
                                     .SelectMany(r => r.Analyze(semanticModel)));
            }
            else
            {
                if (this.LinterVerbose)
                {
                    diagnostics.Add(
                        new AnalyzerDiagnostic(AnalyzerName,
                                new TextSpan(0, 0),
                                DiagnosticLevel.Info,
                                "Linter Disabled",
                                string.Format(CoreResources.LinterDisabledFormatMessage, this.configHelper.CustomSettingsFileName),
                                null, null));
                }
            }
            return diagnostics;
        }

        private IDiagnostic GetConfigurationDiagnostic()
        {
            var configMessage = this.configHelper.CustomSettingsFileName == default ?
                                    CoreResources.BicepConfigNoCustomSettingsMessage
                                    : string.Format(CoreResources.BicepConfigCustomSettingsFoundFormatMessage, this.configHelper.CustomSettingsFileName);

            return new AnalyzerDiagnostic(AnalyzerName,
                                            new TextSpan(0, 0),
                                            DiagnosticLevel.Info,
                                            "Bicep Linter Configuration",
                                            configMessage,
                                            null, null);
        }

        internal IDiagnostic CreateInternalErrorDiagnostic(string analyzerName, string message)
        {
            return new AnalyzerDiagnostic(
                    analyzerName,
                    new TextSpan(0, 0),
                    DiagnosticLevel.Warning,
                    LinterAnalyzer.FailedRuleCode,
                    message,
                    null,
                    null);
        }
    }
}
