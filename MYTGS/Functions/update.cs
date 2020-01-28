using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Deployment.Application;
using System.Windows.Forms;
using System.ComponentModel;
using System.Windows.Media;

namespace MYTGS
{
    public partial class MainWindow
    {

        long sizeOfUpdate = 0;

        public void UpdateApplication()
        {
            if (ApplicationDeployment.IsNetworkDeployed)
            {
                ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;

                //Clear existing event hooks
                ad.CheckForUpdateProgressChanged -= new DeploymentProgressChangedEventHandler(ad_CheckForUpdateProgressChanged);
                ad.CheckForUpdateCompleted -= new CheckForUpdateCompletedEventHandler(ad_CheckForUpdateCompleted);

                //Add event hooks
                ad.CheckForUpdateCompleted += new CheckForUpdateCompletedEventHandler(ad_CheckForUpdateCompleted);
                ad.CheckForUpdateProgressChanged += new DeploymentProgressChangedEventHandler(ad_CheckForUpdateProgressChanged);

                ad.CheckForUpdateAsync();
            }
            else
            {
                UpdateButton.IsEnabled = true;
            }
        }

        void ad_CheckForUpdateProgressChanged(object sender, DeploymentProgressChangedEventArgs e)
        {
            ProgressLabel.Content = String.Format("Checking for update: {0}. {1:D}K of {2:D}K downloaded.", GetProgressString(e.State), e.BytesCompleted / 1024, e.BytesTotal / 1024);
            SettingsProgressBar.Value = e.ProgressPercentage;
        }

        string GetProgressString(DeploymentProgressState state)
        {
            if (state == DeploymentProgressState.DownloadingApplicationFiles)
            {
                return "application files";
            }
            else if (state == DeploymentProgressState.DownloadingApplicationInformation)
            {
                return "application manifest";
            }
            else
            {
                return "deployment manifest";
            }
        }

        void ad_CheckForUpdateCompleted(object sender, CheckForUpdateCompletedEventArgs e)
        {            
            //#FF06B025 default green progress bar colour
            if (e.Error != null)
            {
                SettingsProgressBar.Foreground = Brushes.Red;
                ProgressLabel.Content = "ERROR: Couldn't retrieve new version of application.";
                logger.Warn("Unable to retrieve new version of application");
                logger.Error(e.Error);
                //MessageBox.Show("ERROR: Could not retrieve new version of the application. Reason: \n" + e.Error.Message + "\nPlease report this error to the system administrator.");
                return;
            }
            else if (e.Cancelled == true)
            {
                logger.Warn("Update Check was cancelled");
                //MessageBox.Show("The update was cancelled.");
            }

            // Ask the user if they would like to update the application now.
            if (e.UpdateAvailable)
            {
                sizeOfUpdate = e.UpdateSizeBytes;
                if (!e.IsUpdateRequired)
                {
                    DialogResult dr = MessageBox.Show("An update is available. Would you like to update the application now?", "Update Available", MessageBoxButtons.OKCancel);
                    logger.Info("Update detected, requesting user confirmation");
                    if (System.Windows.Forms.DialogResult.OK == dr)
                    {
                        logger.Info("Update confirmed, Beginning update");
                        BeginUpdate();
                    }
                    else
                    {
                        UpdateButton.IsEnabled = true;
                        ProgressLabel.Content = "";
                        SettingsProgressBar.Value = 0;
                    }
                }
                else
                {
                    logger.Info("Mandatory Update detected, Update will begin");
                    BeginUpdate();
                }
            }
            else
            {
                UpdateButton.IsEnabled = true;
                ProgressLabel.Content = "Application is Up to date!";
                SettingsProgressBar.Value = 0;
                logger.Info("Application is up to date!");
            }
        }

        private void BeginUpdate()
        {
            ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;
            //Clear existing event hooks
            ad.UpdateProgressChanged -= new DeploymentProgressChangedEventHandler(ad_UpdateProgressChanged);
            ad.UpdateCompleted -= new AsyncCompletedEventHandler(ad_UpdateCompleted);

            ad.UpdateCompleted += new AsyncCompletedEventHandler(ad_UpdateCompleted);

            // Indicate progress in the application's status bar.
            ad.UpdateProgressChanged += new DeploymentProgressChangedEventHandler(ad_UpdateProgressChanged);
            ad.UpdateAsync();
        }

        void ad_UpdateProgressChanged(object sender, DeploymentProgressChangedEventArgs e)
        {
            String progressText = String.Format("{0:D}K out of {1:D}K downloaded - {2:D}% complete", e.BytesCompleted / 1024, e.BytesTotal / 1024, e.ProgressPercentage);
            ProgressLabel.Content = progressText;
            SettingsProgressBar.Value = e.ProgressPercentage;
        }

        void ad_UpdateCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                logger.Info("Update was cancelled");
                MessageBox.Show("The update of the application's latest version was cancelled.");
                return;
            }
            else if (e.Error != null)
            {
                logger.Info("Update ran into an error");
                logger.Error(e.Error);
                MessageBox.Show("ERROR: Could not install the latest version of the application. Reason: \n" + e.Error.Message + "\nPlease report this error to the system administrator.");
                return;
            }

            logger.Info("Update Complete");
            DialogResult dr = MessageBox.Show("The application has been updated. Restart? (If you do not restart now, the new version will not take effect until after you quit and launch the application again.)", "Restart Application", MessageBoxButtons.OKCancel);
            if (System.Windows.Forms.DialogResult.OK == dr)
            {
                logger.Info("User confirmed restart for update");
                safeclose = true;
                Application.Restart();
                Close();
            }
            UpdateButton.IsEnabled = true;
            ProgressLabel.Content = "";
            SettingsProgressBar.Value = 0;
        }
    }
}
