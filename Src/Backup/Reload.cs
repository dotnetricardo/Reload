namespace ShapeFx.Shape_Package
{
    using System;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using System.Collections.Generic;

    using EnvDTE;

    using EnvDTE80;

    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using ShapeFX.Reload;
    using ShapeFX.Reload.Forms;
    using System.Threading.Tasks;

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
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    //[ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [Guid(GuidList.guidShape_PackagePkgString)]
    public sealed class Reload : Package
    {
        private Lazy<DTE2> _dte;
        private Lazy<EnvDTE.Events> _events;
        private Lazy<EnvDTE.DocumentEvents> _documentEvents;
        private Lazy<TextDocumentKeyPressEvents> _keyPressEvents;
        private ProjectItemsEvents prjItemsEvents;
        private static string _appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);


        private bool _refreshChrome = false;
        private bool _refreshFirefox = false;
        private bool _refreshIE = false;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>

        public Reload()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
            _dte = new Lazy<DTE2>(() => ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE)) as DTE2);
            _events = new Lazy<EnvDTE.Events>(() => _dte.Value.Events);
            _documentEvents = new Lazy<EnvDTE.DocumentEvents>(() => _events.Value.DocumentEvents);
            //_keyPressEvents = new Lazy<TextDocumentKeyPressEvents>(() => ((Events2)this._dte.Value.Application.Events).TextDocumentKeyPressEvents[null]);
            //_projectItemEvents = new Lazy<EnvDTE.ProjectItemsEvents>(() => _events.Value.SolutionItemsEvents);
        }

        private void HandleCommands(OleMenuCommandService mcs)
        {
            var showLicenseMenuCommandId = new CommandID(GuidList.guidShape_PackageCmdSet, (int)PkgCmdIDList.License);
            var chromeCommandId = new CommandID(GuidList.guidShape_PackageCmdSet, (int)PkgCmdIDList.Chrome);
            var firefoxId = new CommandID(GuidList.guidShape_PackageCmdSet, (int)PkgCmdIDList.Firefox);
            var internetExplorerId = new CommandID(GuidList.guidShape_PackageCmdSet, (int)PkgCmdIDList.IE);
            var webpageCommandId = new CommandID(GuidList.guidShape_PackageCmdSet, (int)PkgCmdIDList.Webpage);
            var aboutCommandId = new CommandID(GuidList.guidShape_PackageCmdSet, (int)PkgCmdIDList.About);

            var showLicenseMenuItem = new MenuCommand(this.ShowLicenceInfo, showLicenseMenuCommandId);
            mcs.AddCommand(showLicenseMenuItem);

            var chromeCommandMenuItem = new MenuCommand(this.RefreshHandler, chromeCommandId);
            mcs.AddCommand(chromeCommandMenuItem);

            var firefoxCommandMenuItem = new MenuCommand(this.RefreshHandler, firefoxId);
            mcs.AddCommand(firefoxCommandMenuItem);

            var internetExplorerCommandMenuItem = new MenuCommand(this.RefreshHandler, internetExplorerId);
            mcs.AddCommand(internetExplorerCommandMenuItem);

            var webpageMenuItem = new MenuCommand(this.GoToWebsite, webpageCommandId);
            mcs.AddCommand(webpageMenuItem);

            var aboutMenuItem = new MenuCommand(this.ShowAbout, aboutCommandId);
            mcs.AddCommand(aboutMenuItem);

        }



        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            //_keyPressEvents.Value.AfterKeyPress += Value_AfterKeyPress;
            _documentEvents.Value.DocumentSaved += DocumentEvents_DocumentSaved;

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                //CommandID menuCommandID = new CommandID(GuidList.guidShape_PackageCmdSet, (int)PkgCmdIDList.shapeCMD);
                //MenuCommand menuItem = new MenuCommand(ShowLicenceInfo, menuCommandID );
                //mcs.AddCommand( menuItem );
                HandleCommands(mcs);
            }
        }

        //void Value_AfterKeyPress(string Keypress, TextSelection Selection, bool InStatementCompletion)
        //{

        //    //if(InStatementCompletion)
        //    //{
        //        _dte.Value.ActiveDocument.Save();
        //        this.Refresh(this._refreshChrome, this._refreshFirefox, this._refreshIE);
                
        //        //Selection.MoveToPoint(Selection.ActivePoint);
        //    //}
        //}


        #endregion

        private bool IsWebProject(Project project)
        {
            return ProjectHasExtender(project, "WebApplication");
        }
        private static bool ProjectHasExtender(Project proj, string extenderName)
        {
            // See http://www.mztools.com/articles/2007/mz2007014.aspx for more information.

            try
            {
                // We could use proj.Extender(extenderName) but it causes an exception if not present and 
                // therefore it can cause performance problems if called multiple times. We use instead:

                var extenderNames = (object[])proj.ExtenderNames;

                return extenderNames.Length > 0 && extenderNames.Any(extenderNameObject => extenderNameObject.ToString() == extenderName);
            }
            catch
            {
                // Ignore
            }

            return false;
        }

        private void DocumentEvents_DocumentSaved(EnvDTE.Document document)
        {
            var textDoc = (TextDocument)document.Object("TextDocument");
            var editPoint = textDoc.StartPoint.CreateEditPoint();
            var text = editPoint.GetText(textDoc.EndPoint);
            var isweb = this.IsWebProject(document.ProjectItem.ContainingProject);
            var selection = textDoc.Selection.ActivePoint;



            //if ((document.FullName.Contains(".shape") && text.Contains("[Sync]")) || (document.FullName.Contains(".cs") && text.Contains("[Sync]") && isweb) || document.FullName.Contains(".cshtml") || document.FullName.Contains(".html"))
            var extensions = Statics.Extensions.Split(',');
            var refresh = extensions.Any(extension => document.FullName.Contains(extension));

            if (refresh)
            {
                this.Refresh(this._refreshChrome, this._refreshFirefox, this._refreshIE);

                document.ActiveWindow.Activate();
                document.ActiveWindow.SetFocus();
                document.Activate();
                textDoc.Selection.MoveToPoint(selection);

            }
        }

        private void Refresh(bool chrome, bool ff, bool ie)
        {
            var script = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "refresh.vbs");

            var host = @"c:\windows\system32\cscript.exe";
            var regex = @"([^\\]+)(?=\.sln)";
            var solution = Regex.Match(_dte.Value.Solution.FileName, regex).Value;



            if (File.Exists(host) && File.Exists(script))
            {
                using (var process = new System.Diagnostics.Process())
                {
                    //process.EnableRaisingEvents = true;

                    //process.Disposed += (e, args) =>
                    //    { System.Threading.Thread.Sleep(2000); };

                    var startInfo = new ProcessStartInfo
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = host,
                        Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\"", script, chrome ? "Google Chrome" : null, solution, ff ? "Firefox" : null, ie ? "Internet Explorer" : null),
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    process.StartInfo = startInfo;
                    process.Start();
                }
            }
        }

        private void RefreshHandler(object sender, EventArgs e)
        {
            var command = sender as System.ComponentModel.Design.MenuCommand;
            command.Checked = !command.Checked;

            switch (command.CommandID.ID)
            {
                case 320:
                    this._refreshChrome = command.Checked;
                    break;
                case 336:
                    this._refreshFirefox = command.Checked;
                    break;
                case 352:
                    this._refreshIE = command.Checked;
                    break;
            }
        }

        private void ShowAbout(object sender, EventArgs e)
        {
            var aboutFrm = new AboutFrm();
            aboutFrm.ShowInTaskbar = false;
            aboutFrm.StartPosition = FormStartPosition.CenterParent;

            using (aboutFrm)
            {
                aboutFrm.ShowDialog();
            }
        }

        private void GoToWebsite(object sender, EventArgs e)
        {
            using (var proc = new System.Diagnostics.Process())
            {
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.FileName = "http://shapeframework.net";
                proc.Start();
            }
        }

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowLicenceInfo(object sender, EventArgs e)
        {

            //// Show a Message Box to prove we were here
            //IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            //Guid clsid = Guid.Empty;
            //int result;
            //Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
            //           0,
            //           ref clsid,
            //           "Shape",
            //           string.Format(CultureInfo.CurrentCulture, "Inside {0}.ShowLicenceInfo()", this.ToString()),
            //           string.Empty,
            //           0,
            //           OLEMSGBUTTON.OLEMSGBUTTON_OK,
            //           OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
            //           OLEMSGICON.OLEMSGICON_INFO,
            //           0,        // false
            //           out result));
        }

    }
}
