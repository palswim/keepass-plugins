using System;
using System.Text;

using KeePass.Plugins;

namespace CredentialAutomation
{
	public sealed class CredentialAutomationExt : Plugin
	{
		KeePass.Forms.MainForm m_formMain = null;
		KeePass.Util.CommandLineArgs m_argsCommand = null;

		public override bool Initialize(IPluginHost host)
		{
			m_formMain = host.MainWindow;
			m_argsCommand = host.CommandLineArgs;
			// I couldn't find any better event to capture
			host.MainWindow.UserActivityPost += DetectLoadingHasCompleted;
			return true;
		}

		void DetectLoadingHasCompleted(object sender, EventArgs e)
		{
			KeePassLib.Interfaces.IStatusLogger loggerSW = null;
			KeePassLib.Keys.CompositeKey key = null;

			foreach (var doc in m_formMain.DocumentManager.Documents)
			{
				if (doc.Database.IsOpen || string.IsNullOrEmpty(doc.LockedIoc.Path))
					// If the DB is Open, no need to re-open it
					// If the path is blank, then we've captured an event too early in the process
					continue;

				if (loggerSW == null)
				{	// Initialize the data a single time
					key = KeePass.Util.KeyUtil.KeyFromCommandLine(m_argsCommand);
					m_formMain.UserActivityPost -= DetectLoadingHasCompleted; // Unsubscribe from any more events
					if (key == null)
						return; // Do this after removing the event handler, since we don't have any Key to pass
					loggerSW = m_formMain.CreateShowWarningsLogger();
					loggerSW.StartLogging(KeePass.Resources.KPRes.OpenDatabase, true);
				}
				doc.Database.Open(doc.LockedIoc, key, loggerSW); // Attempt to open with command-line credentials
				if (doc.Database.IsOpen)
					doc.LockedIoc = new KeePassLib.Serialization.IOConnectionInfo(); // Clear lock
			}

			if (loggerSW != null)
			{
				loggerSW.EndLogging();
			}
		}

		public override void Terminate()
		{
			m_formMain.UserActivityPost -= DetectLoadingHasCompleted;
		}
	}
}
