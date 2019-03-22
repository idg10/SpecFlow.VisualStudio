using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using BoDi;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using TechTalk.SpecFlow.IdeIntegration.Install;
using TechTalk.SpecFlow.BindingSkeletons;
using TechTalk.SpecFlow.IdeIntegration.Analytics;
using TechTalk.SpecFlow.IdeIntegration.Options;
using TechTalk.SpecFlow.IdeIntegration.Tracing;
using TechTalk.SpecFlow.VsIntegration.Analytics;
using TechTalk.SpecFlow.VsIntegration.Install;
using TechTalk.SpecFlow.VsIntegration.LanguageService;
using TechTalk.SpecFlow.VsIntegration.Options;
using TechTalk.SpecFlow.VsIntegration.TestRunner;
using TechTalk.SpecFlow.VsIntegration.Tracing;
using TechTalk.SpecFlow.VsIntegration.Tracing.OutputWindow;
using TechTalk.SpecFlow.VsIntegration.Utils;

namespace TechTalk.SpecFlow.VsIntegration
{
    public static class VsContainerBuilder
    {
        internal static DefaultDependencyProvider DefaultDependencyProvider = new DefaultDependencyProvider();

        public static async System.Threading.Tasks.Task<IObjectContainer> CreateContainer(SpecFlowPackagePackage package)
        {
            var container = new ObjectContainer();

            container.RegisterInstanceAs(package);
            container.RegisterInstanceAs<IServiceProvider>(package);
            container.RegisterInstanceAs<IAsyncServiceProvider>(package);

            await RegisterDefaults(container);

            BiDiContainerProvider.CurrentContainer = container; //TODO: avoid static field

            return container;
        }

        private static System.Threading.Tasks.Task RegisterDefaults(IObjectContainer container)
        {
            return DefaultDependencyProvider.RegisterDefaults(container);
        }
    }

    internal partial class DefaultDependencyProvider
    {
        static partial void RegisterCommands(IObjectContainer container);

        public virtual async System.Threading.Tasks.Task RegisterDefaults(IObjectContainer container)
        {
            var serviceProvider = container.Resolve<IAsyncServiceProvider>();
            await RegisterVsDependencies(container, serviceProvider);

            container.RegisterTypeAs<InstallServices, InstallServices>();
            container.RegisterTypeAs<InstallServicesHelper, InstallServicesHelper>();
            container.RegisterTypeAs<VsBrowserGuidanceNotificationService, IGuidanceNotificationService>();
            container.RegisterTypeAs<WindowsFileAssociationDetector, IFileAssociationDetector>();
            container.RegisterTypeAs<RegistryStatusAccessor, IStatusAccessor>();

            container.RegisterTypeAs<PackageIntegrationOptionsProvider, IIntegrationOptionsProvider>();
            container.RegisterInstanceAs<IIdeTracer>(await VsxHelper.ResolveMefDependencyAsync<IVisualStudioTracer>(serviceProvider));
            container.RegisterInstanceAs(await VsxHelper.ResolveMefDependencyAsync<IProjectScopeFactory>(serviceProvider));


            container.RegisterTypeAs<StepDefinitionSkeletonProvider, IStepDefinitionSkeletonProvider>();
            container.RegisterTypeAs<DefaultSkeletonTemplateProvider, ISkeletonTemplateProvider>();
            container.RegisterTypeAs<StepTextAnalyzer, IStepTextAnalyzer>();

            container.RegisterTypeAs<ConsoleAnalyticsTransmitterSink, IAnalyticsTransmitterSink>();
            container.RegisterTypeAs<AnalyticsTransmitter, IAnalyticsTransmitter>();
            container.RegisterTypeAs<EnableAnalyticsChecker, IEnableAnalyticsChecker>();
            container.RegisterTypeAs<RegistryUserUniqueIdStore, IUserUniqueIdStore>();

            RegisterCommands(container);
        }

        protected virtual async System.Threading.Tasks.Task RegisterVsDependencies(IObjectContainer container, IAsyncServiceProvider serviceProvider)
        {
            var dte = await serviceProvider.GetServiceAsync(typeof(DTE)) as DTE;
            if (dte != null)
            {
                container.RegisterInstanceAs(dte);
                container.RegisterInstanceAs((DTE2)dte);
            }

            container.RegisterInstanceAs(await VsxHelper.ResolveMefDependencyAsync<IOutputWindowService>(serviceProvider));
            container.RegisterInstanceAs(await VsxHelper.ResolveMefDependencyAsync<IGherkinLanguageServiceFactory>(serviceProvider));
        }
    }

    public interface IBiDiContainerProvider
    {
        IObjectContainer ObjectContainer { get; }
    }

    [Export(typeof(IBiDiContainerProvider))]
    internal class BiDiContainerProvider : IBiDiContainerProvider
    {
        public static IObjectContainer CurrentContainer { get; internal set; }

        public IObjectContainer ObjectContainer
        {
            get { return CurrentContainer; }
        }
    }
}
