using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using BoDi;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using TechTalk.SpecFlow.IdeIntegration.Install;
using TechTalk.SpecFlow.VsIntegration.Commands;
using TechTalk.SpecFlow.VsIntegration.Options;
using TechTalk.SpecFlow.VsIntegration.Utils;
using System.Threading;
using System.Threading.Tasks;

using Task = System.Threading.Tasks.Task;

namespace TechTalk.SpecFlow.VsIntegration
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    // This attribute is used to register the information needed to show that this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", GuidList.ProductId, IconResourceID = 400)]
    [ProvideOptionPage(typeof(OptionsPageGeneral), IntegrationOptionsProvider.SPECFLOW_OPTIONS_CATEGORY, IntegrationOptionsProvider.SPECFLOW_GENERAL_OPTIONS_PAGE, 121, 122, true)]
    [ProvideProfile(typeof(OptionsPageGeneral), IntegrationOptionsProvider.SPECFLOW_OPTIONS_CATEGORY, IntegrationOptionsProvider.SPECFLOW_GENERAL_OPTIONS_PAGE, 121, 123, true, DescriptionResourceID = 121)]
    [Guid(GuidList.guidSpecFlowPkgString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class SpecFlowPackagePackage : AsyncPackage
    {
        public IObjectContainer Container { get; private set; }

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public SpecFlowPackagePackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));


            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
        }

        private void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (args.LoadedAssembly.GetName().Name.StartsWith("TechTalk.SpecFlow") && args.LoadedAssembly.Location.Contains("\\bin\\Debug"))
            {
                Debugger.Break();
            }

        }

        public static IdeIntegration.Install.IdeIntegration? CurrentIdeIntegration
        {
            get
            {
                switch(VSVersion.FullVersion.Major)
                {
                    case 12:
                        return IdeIntegration.Install.IdeIntegration.VisualStudio2013;
                    case 14:
                        return IdeIntegration.Install.IdeIntegration.VisualStudio2015;
                    case 15:
                        return IdeIntegration.Install.IdeIntegration.VisualStudio2017;
                    
                }
                return IdeIntegration.Install.IdeIntegration.Unknown;
            }
        }

        public static string AssemblyName
        {
            get
            {
                switch (CurrentIdeIntegration)
                {
                    case IdeIntegration.Install.IdeIntegration.VisualStudio2013:
                        return "TechTalk.SpecFlow.VsIntegration.2013";
                    case IdeIntegration.Install.IdeIntegration.VisualStudio2015:
                        return "TechTalk.SpecFlow.VsIntegration.2015";
                    case IdeIntegration.Install.IdeIntegration.VisualStudio2017:
                        return "TechTalk.SpecFlow.VsIntegration.2017";
                    default:
                        return "TechTalk.SpecFlow.VsIntegration";
                }
            }
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that relies on services provided by VisualStudio.
        /// </summary>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            await base.InitializeAsync(cancellationToken, progress);

            Container = await VsContainerBuilder.CreateContainer(this);

            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var settings = (OptionsPageGeneral)GetDialogPage(typeof(OptionsPageGeneral));
            Container.RegisterInstanceAs(settings);
            await TaskScheduler.Default;


            var currentIdeIntegration = CurrentIdeIntegration;
            if (currentIdeIntegration != null)
            {
                await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                // This resolution needs to happen on the main thread because one of the
                // dependencies involved is DTE. That's a COM interface, and as part of
                // passing that as a ctor argument to the VsBrowserGuidanceNotificationService
                // that needs it, reflection will want to check that the object it has implements
                // DTE. That will entail calling QueryInterface under the covers, and since the
                // DTE object is STA-bound, that in turn means hitting the UI thread. And this
                // seems to cause deadlock. So better to force ourselves onto the UI thread now.
                InstallServices installServices = Container.Resolve<InstallServices>();
                installServices.OnPackageLoad(currentIdeIntegration.Value);
                await TaskScheduler.Default;
            }


            OleMenuCommandService menuCommandService = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (menuCommandService != null)
            {
                await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                foreach (var menuCommandHandler in Container.Resolve<IDictionary<SpecFlowCmdSet, MenuCommandHandler>>())
                {
                    menuCommandHandler.Value.RegisterTo(menuCommandService, menuCommandHandler.Key);
                }
            }
        }
    }
}
