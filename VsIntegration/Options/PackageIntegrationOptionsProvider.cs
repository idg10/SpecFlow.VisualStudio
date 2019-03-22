using TechTalk.SpecFlow.IdeIntegration.Options;

namespace TechTalk.SpecFlow.VsIntegration.Options
{
    /// <summary>
    /// Options provider suitable for use during package load.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Due to supporting asynchronous package load, we need to be more flexible about how we
    /// load options. The approach used by <see cref="IntegrationOptionsProvider"/> doesn't work
    /// during asynchronous package load. It reads settings from DTE. If you try that from a
    /// non-UI thread, it will fail. And it seems that if you first switch to the UI thread, it
    /// deadlocks for some reason.
    /// </para>
    /// <para>
    /// The preferred way for a package to access settings is to read them out of the same class
    /// that defines them for the options page - <see cref="OptionsPageGeneral"/> in our case.
    /// The package can simply ask for this during load, and we can read the properties out of
    /// it directly. This is much easier than going through DTE because we get strongly-typed
    /// access to our properties. However, we can't use this for MEF components, because there's
    /// no guarantee that our package will have finished loading by the time VS starts to
    /// instantiate our MEF components. So for anything operating in that world, we have to
    /// carry on using the DTE-based approach.
    /// </para>
    /// </remarks>
    internal class PackageIntegrationOptionsProvider : IIntegrationOptionsProvider
    {
        private IntegrationOptions options;

        public PackageIntegrationOptionsProvider(OptionsPageGeneral page)
        {
            int maxStepInstancesSuggestions;
            options = new IntegrationOptions
            {
                EnableSyntaxColoring = page.EnableSyntaxColoring,
                EnableOutlining = page.EnableOutlining,
                EnableIntelliSense = page.EnableIntelliSense,
                LimitStepInstancesSuggestions = int.TryParse(page.MaxStepInstancesSuggestions, out maxStepInstancesSuggestions),
                MaxStepInstancesSuggestions = maxStepInstancesSuggestions,
                EnableAnalysis = page.EnableAnalysis,
                EnableTableAutoFormat = page.EnableTableAutoFormat,
                EnableStepMatchColoring = page.EnableStepMatchColoring,
                EnableTracing = page.EnableTracing,
                TracingCategories = page.TracingCategories,
                DisableRegenerateFeatureFilePopupOnConfigChange = page.DisableRegenerateFeatureFilePopupOnConfigChange,
                GenerationMode = page.GenerationMode,
                CodeBehindFileGeneratorPath = page.PathToCodeBehindGeneratorExe,
                CodeBehindFileGeneratorExchangePath = page.CodeBehindFileGeneratorExchangePath,
                OptOutDataCollection = page.OptOutDataCollection
            };

        }

        public IntegrationOptions GetOptions()
        {
            return options;
        }
    }
}
