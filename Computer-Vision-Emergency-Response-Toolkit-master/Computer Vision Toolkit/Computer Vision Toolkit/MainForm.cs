﻿//=============================================================================================
//=============================================================================================
/*
                                License Agreement
                  For Computer Vision Emergency Response Toolkit
                              (BSD 3-Clause License)
    
    Copyright(c) 2018, Texas A&M Engineering Experiment Station
    All rights reserved.
    
    Redistribution and use in source and binary forms, with or without
    modification, are permitted provided that the following conditions are met:
    
    * Redistributions of source code must retain the above copyright notice, this
      list of conditions and the following disclaimer.
    
    * Redistributions in binary form must reproduce the above copyright notice,
      this list of conditions and the following disclaimer in the documentation
      and/or other materials provided with the distribution.
    
    * Neither the name of the copyright holder nor the names of its
      contributors may be used to endorse or promote products derived from
      this software without specific prior written permission.
    
    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
    AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
    IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
    DISCLAIMED.IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
    FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
    DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
    SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
    CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
    OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
    OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

*/
//=============================================================================================
//=============================================================================================





using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using SystemLog;


namespace Computer_Vision_Toolkit
{

    //Global Settings
    public struct Settings      //NOTE: THIS NEEDS TO BE CONVERTED TO USING THE NATIVE SETTINGS MANAGER WITHIN C# APPLICATIONS
    {
        public bool FirstRun;
        public bool RunPOST;
        public string PythonPath;
        public string BatchesPath;
        public string LibPath;
        public bool NewImageWindow;
        public bool AllowMultiThread;

    }


    public partial class MainForm : Form
    {
        //Logging System
        public ErrorLog elog = new ErrorLog();
        public StatusLog slog = new StatusLog();

        //NOTE: "settings.ini" NEEDS TO BE CONVERTED TO USING THE NATIVE SETTINGS MANAGER WITHIN C# APPLICATIONS
        //Settings
        public Settings settings = new Settings();     //Values read from the 'settings.ini'
        public string settingsPath = @"settings.ini";

        //Result viewing variables
        public string selectResultsFolder = "";
        public List<String> currentImages = new List<String>();
        public List<string> checked_images = new List<string>();
        public List<string> flagged_images = new List<string>();
        public List<string> image_stats = new List<string>();
        public int previousSelectedIndex = 0;
        public bool allow_checked = false;

        public Process proc_img1;
        public Process proc_img2;

        public bool pbox1_left = false;
        public bool pbox2_left = false;
        public bool pbox1_right = false;
        public bool pbox2_right = false;

        public Color centerColor = Color.FromArgb(0, 0, 0, 0);
        public Color[] colors = { Color.FromArgb(255, 0, 0, 0) };
        public SolidBrush brush = new SolidBrush(Color.FromArgb(150, 0, 0, 0));
        public GraphicsPath path = new GraphicsPath();
        public Graphics g1;
        public Graphics g2;

        //Mouse location
        public int mouse_x;
        public int mouse_y;

        //===================================================================================================================
        //-------------------------------------------------------------------------------------------------------------------
        //===================================================================================================================

        public MainForm()
        {
            InitializeComponent();

            //Read settings.ini
            ReadSettings();

            if( settings.FirstRun || settings.RunPOST ) POST();

            //Update settings.ini
            UpdateSettings();
            
        }

        //===================================================================================================================
        //------------------------------------------------Read Settings File-------------------------------------------------
        //===================================================================================================================

        private void ReadSettings()     //NOTE: THIS NEEDS TO BE CONVERTED TO USING THE NATIVE SETTINGS MANAGER WITHIN C# APPLICATIONS
        {
            try
            {
                string[] entries = File.ReadAllLines(settingsPath);

                foreach (string str in entries)
                {
                    string[] opt = str.Split('=');
                    switch (opt[0])
                    {
                        case "FirstRun":
                            {
                                settings.FirstRun = (!string.IsNullOrEmpty(opt[1])) ? Convert.ToBoolean(opt[1]) : true;

                                //TODO***RESET ALL PATH STRINGS

                                break;
                            }

                        case "RunPOST":
                            {
                                settings.RunPOST = (!string.IsNullOrEmpty(opt[1])) ? Convert.ToBoolean(opt[1]) : false;
                                break;
                            }

                        case "PythonPath":
                            {
                                settings.PythonPath = opt[1];

                                if (string.IsNullOrEmpty(settings.PythonPath) || !File.Exists(settings.PythonPath))
                                {
                                    settings.FirstRun = true;

                                }
                                break;
                            }

                        case "BatchesPath":
                            {
                                settings.BatchesPath = opt[1];
                                if (string.IsNullOrEmpty(settings.BatchesPath) || !File.Exists(settings.BatchesPath))
                                {
                                    //settings.BatchesPath = Path.Combine(Environment.CurrentDirectory, "Batches");
                                    settings.BatchesPath = Environment.ExpandEnvironmentVariables(@"%userprofile%\Documents\Computer Vision Emergency Response Toolkit\Batches");
                                }
                                break;
                            }
                        case "LibPath":
                            {
                                settings.LibPath = opt[1];
                                if (string.IsNullOrEmpty(settings.LibPath) || !File.Exists(settings.LibPath))
                                {
                                    settings.LibPath = Path.Combine(Environment.CurrentDirectory, "lib");
                                }
                                break;
                            }

                        case "NewImageWindow":
                            {
                                //settings.NewImageWindow = (!string.IsNullOrEmpty(opt[1])) ? Convert.ToBoolean(opt[1]) : false;
                                //this.checkBoxNewWindow.Checked = settings.NewImageWindow;

                                break;
                            }

                        case "AllowMultiThread":
                            {
                                settings.AllowMultiThread = (!string.IsNullOrEmpty(opt[1])) ? Convert.ToBoolean(opt[1]) : false;
                                menuOptimizedMode.Text = (settings.AllowMultiThread) ? "Optimized for: Analysis" : "Optimized for: Viewing";
                                break;
                            }

                        default:
                            {
                                break;
                            }
                    }
                }
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //-----------------------------------------------Update Settings File------------------------------------------------
        //===================================================================================================================

        //Update the settings.ini
        private void UpdateSettings()       //NOTE: THIS NEEDS TO BE CONVERTED TO USING THE NATIVE SETTINGS MANAGER WITHIN C# APPLICATIONS
        {
            try
            {
                //string[] output = { settings.FirstRun.ToString(), settings.PythonPath, settings.BatchesPath, settings.BinPath };
                File.WriteAllLines(settingsPath,
                    new string[]{
                    "FirstRun=" + settings.FirstRun.ToString(),
                    "RunPOST=" + settings.RunPOST.ToString(),
                    "PythonPath=" + settings.PythonPath,
                    "BatchesPath=" + settings.BatchesPath,
                    "LibPath=" + settings.LibPath,
                    "AllowMultiThread=" + settings.AllowMultiThread
                    });
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //-----------------------------------------------Read Checkbox File--------------------------------------------------
        //===================================================================================================================

        //Reads the checkbox.ini found in each batch folder
        private List<string> ReadCheckbox(string batch)
        {
            List<string> str = new List<string>();
            try
            {
                str = File.ReadAllLines(batch + @"\checkbox.ini").ToList<string>();
                return str;
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
                return str;
            } 
        }

        //===================================================================================================================
        //----------------------------------------------Update Checkbox File-------------------------------------------------
        //===================================================================================================================

        //Updates the checkbox.ini found in each batch folder
        private void UpdateCheckbox(string batch, string[] str)
        {
            try
            {
                //If the file does not exist, create it
                if (!File.Exists(batch + @"\checkbox.ini")) File.Create(batch + @"\checkbox.ini").Close();

                File.WriteAllLines(batch + @"\checkbox.ini", str);
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }


        //===================================================================================================================
        //-----------------------------------------------Read Flagged File--------------------------------------------------
        //===================================================================================================================

        //Reads the checkbox.ini found in each batch folder
        private List<string> ReadFlagged(string batch)
        {
            List<string> str = new List<string>();
            try
            {
                //If the file does not exist, create it
                if (!File.Exists(batch + @"\flagged.ini")) File.Create(batch + @"\flagged.ini").Close();

                str = File.ReadAllLines(batch + @"\flagged.ini").ToList<string>();
                return str;
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
                return str;
            }
        }

        //===================================================================================================================
        //----------------------------------------------Update Flagged File-------------------------------------------------
        //===================================================================================================================

        //Updates the flagged.ini found in each batch folder
        private void UpdateFlagged(string batch, string[] str)
        {
            try
            {
                //If the file does not exist, create it
                if(!File.Exists(batch + @"\flagged.ini")) File.Create(batch + @"\flagged.ini").Close();
                File.WriteAllLines(batch + @"\flagged.ini", str);
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //--------------------------------------------Convert String To Color------------------------------------------------
        //===================================================================================================================

        private Color StringToColor(string str)
        {
            try
            {
                switch(str)
                {
                    case "Red": return Color.Red;
                    case "Orange": return Color.Orange;
                    case "Yellow": return Color.Yellow;
                    case "Green": return Color.Green;
                    case "Blue": return Color.Blue;
                    case "Purple": return Color.Purple;
                    case "Gray": return Color.Gray;

                    default: return Color.White;
                }
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
                return Color.White;
            }
        }


        //===================================================================================================================
        //-----------------------------------------------Read Stats File--------------------------------------------------
        //===================================================================================================================

        //Reads the detected_log.txt found in each batch folder
        private List<string> ReadImageStats(string batch)
        {
            List<string> str = new List<string>();
            try
            {
                str = File.ReadAllLines(batch + @"\detected_log.txt").ToList<string>();
                return str;
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
                return str;
            }
        }

        //===================================================================================================================
        //-----------------------------------------------------POST----------------------------------------------------------
        //===================================================================================================================

        //Power On Self Test
        private void POST()
        {
            try
            {
                //Disable button while the program checks for python
                menuBtnNewAnalysis.Enabled = false;

                PythonCheckForm py = new PythonCheckForm(settings);
                py.ShowDialog();
                settings = py.GetSettings();

                //Test if python is installed
                if (py.DialogResult == DialogResult.No)
                {
                    menuBtnNewAnalysis.Enabled = false;
                    btnNewAnalysis.Enabled = false;
                    settings.FirstRun = true;
                }
                else
                {
                    menuBtnNewAnalysis.Enabled = true;
                    btnNewAnalysis.Enabled = true;
                    settings.FirstRun = false;
                }
           
                UpdateSettings();
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //---------------------------------------------------Display Images--------------------------------------------------
        //===================================================================================================================

        private void displayImages(bool use_window)
        {
            try
            {
                string[] extensions = { "jpeg", "jpg", "png" };

                //Display initial image in list
                if (!use_window)
                {
                   
                    //find image with selected checkbox item and show it in pictureBoxes 
                    foreach (var ext in extensions)
                    {
                        if (dataGridView1.SelectedRows != null && checkImages(Path.ChangeExtension(dataGridView1.SelectedRows[0].Cells["Image"].Value.ToString(), ext), 1) == true)
                        {
                            pictureBox2.ImageLocation = Path.Combine(selectResultsFolder, "Analyzed", Path.ChangeExtension(dataGridView1.SelectedRows[0].Cells["Image"].Value.ToString(), ext));
                            break;
                        }
                        else
                            pictureBox2.ImageLocation = "";
                    }

                    foreach (var ext in extensions)
                    {
                        if (dataGridView1.SelectedRows != null && checkImages(Path.ChangeExtension(dataGridView1.SelectedRows[0].Cells["Image"].Value.ToString(), ext), 0) == true)
                        {
                            pictureBox1.ImageLocation = Path.Combine(selectResultsFolder, "Original", Path.ChangeExtension(dataGridView1.SelectedRows[0].Cells["Image"].Value.ToString(), ext));
                            break;
                        }
                        else
                            pictureBox1.ImageLocation = "";
                    }

                    pictureBox1.Update();
                    pictureBox2.Update();

                    g1 = Graphics.FromImage(pictureBox1.Image);
                    g2 = Graphics.FromImage(pictureBox2.Image);
                    path.Reset();

                }
                else
                {
                   
                    //Find the file of the selected image and show the image in its own window 
                    foreach (var ext in extensions)
                        if (dataGridView1.SelectedRows != null && checkImages(Path.ChangeExtension(dataGridView1.SelectedRows[0].Cells["Image"].Value.ToString(), ext), 1) == true)
                        {
                            Process.Start(Path.Combine(selectResultsFolder, "Analyzed", Path.ChangeExtension(dataGridView1.SelectedRows[0].Cells["Image"].Value.ToString(), ext)));
                        }

                    foreach (var ext in extensions)
                        if (dataGridView1.SelectedRows != null && checkImages(Path.ChangeExtension(dataGridView1.SelectedRows[0].Cells["Image"].Value.ToString(), ext), 0) == true)
                        {
                            Process.Start(Path.Combine(selectResultsFolder, "Original", Path.ChangeExtension(dataGridView1.SelectedRows[0].Cells["Image"].Value.ToString(), ext)));
                        }
                }
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //-------------------------------------------------Analysis Button Click---------------------------------------------
        //===================================================================================================================

        private void menuBtnNewAnalysis_Click(object sender, EventArgs e)
        {
            try
            {
                ProcessForm proc_form = new ProcessForm(settings);
                proc_form.Show();
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //-------------------------------------------------Results Button Click----------------------------------------------
        //===================================================================================================================

        private void menuBtnSelectResults_Click(object sender, EventArgs e)
        {
            try
            {

                resultDialog results = new resultDialog(settings);
                results.ShowDialog();

                if (results.DialogResult == DialogResult.OK && !string.IsNullOrEmpty(results.getSelected()))
                {
                    pictureBox1.ImageLocation = "";
                    pictureBox2.ImageLocation = "";
                    previousSelectedIndex = 0;

                    selectResultsFolder = results.getSelected();

                    //Update form title to the path of the batch being viewed
                    this.Text = selectResultsFolder;

                    //Load the detected images
                    this.loadImages();

                    //Allows the user to view a batch folder that is currently being analyzed
                    FileSystemWatcher watcher = new FileSystemWatcher(Path.Combine(selectResultsFolder, "Analyzed"));
                    watcher.EnableRaisingEvents = true;
                    watcher.NotifyFilter = NotifyFilters.LastWrite;
                    //watcher.Created += Watcher_Created;
                    watcher.Changed += Watcher_Changed;

                    //Display currently selected images
                    displayImages(false);
                
                }
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //-----------------------------------------------Watcher File Changed------------------------------------------------
        //===================================================================================================================

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                //Calls the method with the thread that owns the UI object
                Invoke((MethodInvoker)(() => updateImages()));
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //-----------------------------------------------Watcher File Created------------------------------------------------
        //===================================================================================================================

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            try
            {
                //Calls the method with the thread that owns the UI object
                Invoke((MethodInvoker)(() => updateImages()));
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }


        //===================================================================================================================
        //--------------------------------------------Selected Index Changed-------------------------------------------------
        //===================================================================================================================

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                displayImages(false);
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //---------------------------------------------------Check Images----------------------------------------------------
        //===================================================================================================================

        private bool checkImages(string image, int val)
        {
            if(val == 0)
                return (File.Exists(Path.Combine(selectResultsFolder, "Original", image)));
            if(val == 1)
                return (File.Exists(Path.Combine(selectResultsFolder, "Analyzed", image)));

            return false;
        }

        //===================================================================================================================
        //---------------------------------------------Load Images from Folder-----------------------------------------------
        //===================================================================================================================

        private void loadImages()
        {
            try
            {
                //Read the checkbox.ini
                checked_images = ReadCheckbox(selectResultsFolder);
                flagged_images = ReadFlagged(selectResultsFolder);
                image_stats = ReadImageStats(selectResultsFolder);

                String combined = System.IO.Path.Combine(selectResultsFolder, "Analyzed");
                string[] temp = Directory.GetFiles(combined);

                //Reset current images and checkbox
                currentImages.Clear();
                dataGridView1.Rows.Clear();

                //Ensure images have results and are of right format
                for (int i = 0; i < temp.Length; i++)  
                     if (temp[i].ToLower().EndsWith(".jpg") || temp[i].ToLower().EndsWith(".jpeg") || temp[i].ToLower().EndsWith(".png"))
                        currentImages.Add(Path.GetFileName(temp[i]));


                for (int i=0; i < currentImages.Count; i++)
                {
                    string stat = image_stats.Find((string x) => x.Contains( Path.GetFileNameWithoutExtension(currentImages[i]) ));
                    double image_score = ( !string.IsNullOrEmpty(stat) ) ? Convert.ToDouble( stat.Split(' ').Last<string>() ) : 0.0d;


                    //Create row for dataGridView
                    dataGridView1.Rows.Add( "", "No", currentImages[i], image_score.ToString("N10"));
                    dataGridView1.Rows[i].Cells["Checked"].Style.BackColor = Color.White;
                    dataGridView1.Rows[i].Cells["Image"].Style.BackColor = Color.White;
                    dataGridView1.Rows[i].Cells["Score"].Style.BackColor = Color.White;
                    dataGridView1.Rows[i].Cells["Flag"].Style.ForeColor = Color.White;
                    dataGridView1.Rows[i].Cells["Flag"].Style.BackColor = Color.White;
                    dataGridView1.Rows[i].Cells["Flag"].Style.SelectionBackColor = Color.FromKnownColor(KnownColor.Highlight);
                    dataGridView1.Rows[i].Cells["Flag"].Style.SelectionForeColor = Color.FromKnownColor(KnownColor.Highlight);

                    //Checks previously checked images
                    if (checked_images.Contains(dataGridView1.Rows[i].Cells["Image"].Value))
                    {
                        dataGridView1.Rows[i].Cells["Checked"].Value = "Yes";
                        dataGridView1.Rows[i].Cells["Checked"].Style.BackColor = Color.LightBlue;
                        dataGridView1.Rows[i].Cells["Image"].Style.BackColor = Color.LightBlue;
                        dataGridView1.Rows[i].Cells["Score"].Style.BackColor = Color.LightBlue;
                    }
                    
                    //Check previously flagged images
                    if (flagged_images.Any<string>((string x) =>  x.Split('=')[0].Contains(dataGridView1.Rows[i].Cells["Image"].Value.ToString())))
                    {
                        string color_str = flagged_images.Find((string x) => x.Contains(dataGridView1.Rows[i].Cells["Image"].Value.ToString()) ).Split('=')[1];
                        
                        //Switch for the various colors
                        dataGridView1.Rows[i].Cells["Flag"].Value = color_str;
                        dataGridView1.Rows[i].Cells["Flag"].Style.BackColor = StringToColor(color_str);
                        dataGridView1.Rows[i].Cells["Flag"].Style.ForeColor = StringToColor(color_str);
                        dataGridView1.Rows[i].Cells["Flag"].Style.SelectionBackColor = StringToColor(color_str);
                        dataGridView1.Rows[i].Cells["Flag"].Style.SelectionForeColor = StringToColor(color_str);
                    }
                }
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //-----------------------------------------------Update Image List---------------------------------------------------
        //===================================================================================================================

        private void updateImages()
        {
            try
            {
                String combined = System.IO.Path.Combine(selectResultsFolder, "Analyzed");
                string[] temp = Directory.GetFiles(combined);

                //Update image stats
                image_stats = ReadImageStats(selectResultsFolder);

                //Ensure images have results and are of right format
                for (int i = 0; i < temp.Length; i++)
                {
                    if (temp[i].ToLower().EndsWith(".jpg") || temp[i].ToLower().EndsWith(".jpeg") || temp[i].ToLower().EndsWith(".png"))
                    {
                        if(!currentImages.Contains(Path.GetFileName(temp[i])))
                        {
                            currentImages.Add(Path.GetFileName(temp[i]));

                            //Add the new image to the dataGridView
                            string stat = image_stats.Find((string x) => x.Contains(Path.GetFileNameWithoutExtension( currentImages.Last<string>() )));
                            double image_score = (!string.IsNullOrEmpty(stat)) ? Convert.ToDouble(stat.Split(' ')[3]) : 0.0d;

                           
                            dataGridView1.Rows.Add( "", "No", currentImages.Last<string>(), image_score.ToString());
                            dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells["Checked"].Style.BackColor = Color.White;
                            dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells["Image"].Style.BackColor = Color.White;
                            dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells["Score"].Style.BackColor = Color.White;
                            dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells["Flag"].Style.BackColor = Color.White;
                            dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells["Flag"].Style.ForeColor = Color.White;
                            dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells["Flag"].Style.SelectionBackColor = Color.FromKnownColor(KnownColor.Highlight);
                            dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells["Flag"].Style.SelectionForeColor = Color.FromKnownColor(KnownColor.Highlight);
                        }
                    } 
                }
                
                dataGridView1.Update();
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }
        
        //===================================================================================================================
        //---------------------------------------------Edit Parameters Clicked-----------------------------------------------
        //===================================================================================================================

        private void editParametersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                ParametersForm pf = new ParametersForm();
                pf.ShowDialog();
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //--------------------------------------------View Optimization Clicked----------------------------------------------
        //===================================================================================================================

        private void viewingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                settings.AllowMultiThread = false;
                menuOptimizedMode.Text = "Optimized for: Viewing";
                UpdateSettings();
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //-----------------------------------------Analysis Optimization Clicked---------------------------------------------
        //===================================================================================================================

        private void analysisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                settings.AllowMultiThread = true;
                menuOptimizedMode.Text = "Optimized for: Analysis";
                UpdateSettings();
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //---------------------------------------Open Image in New Window Clicked--------------------------------------------
        //===================================================================================================================

        private void openImagesInNewWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                displayImages(true);
                dataGridView1.Focus();
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //----------------------------------------------Mark Image as Viewed-------------------------------------------------
        //===================================================================================================================

        private void btnMarkAsViewed_Click(object sender, EventArgs e)
        {
            try
            {
               if (dataGridView1.Rows.Count > 0 && dataGridView1.SelectedRows.Count > 0)
                {
                    if( dataGridView1.SelectedRows[0].Cells["Checked"].Value.Equals("No") )
                    {
                        if (!checked_images.Contains(dataGridView1.SelectedRows[0].Cells["Image"].Value.ToString()))
                            checked_images.Add(dataGridView1.SelectedRows[0].Cells["Image"].Value.ToString());

                        //Update dataGridView
                        dataGridView1.SelectedRows[0].Cells["Checked"].Value = "Yes";
                        dataGridView1.SelectedRows[0].Cells["Checked"].Style.BackColor = Color.LightBlue;
                        dataGridView1.SelectedRows[0].Cells["Image"].Style.BackColor = Color.LightBlue;
                        dataGridView1.SelectedRows[0].Cells["Score"].Style.BackColor = Color.LightBlue;

                        //Update the checkbox.ini
                        UpdateCheckbox(selectResultsFolder, checked_images.ToArray());
                    }
                    else
                    {
                        checked_images.Remove(dataGridView1.SelectedRows[0].Cells["Image"].Value.ToString());

                        //Update dataGridView
                        dataGridView1.SelectedRows[0].Cells["Checked"].Value = "No";
                        dataGridView1.SelectedRows[0].Cells["Checked"].Style.BackColor = Color.White;
                        dataGridView1.SelectedRows[0].Cells["Image"].Style.BackColor = Color.White;
                        dataGridView1.SelectedRows[0].Cells["Score"].Style.BackColor = Color.White;

                        //Update the checkbox.ini
                        UpdateCheckbox(selectResultsFolder, checked_images.ToArray());
                    }
                }

            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }

        }

        //===================================================================================================================
        //----------------------------------------------Mark Image as Flagged------------------------------------------------
        //===================================================================================================================

        private void MarkAsFlagged()
        {
            try
            {
                if (dataGridView1.Rows.Count > 0 && dataGridView1.SelectedRows.Count > 0)
                {
                    if (flagged_images.Any<string>((string x) => x.Split('=')[0].Contains(dataGridView1.SelectedRows[0].Cells["Image"].Value.ToString())))
                    {
                        flagged_images.Remove(flagged_images.Find((string x) => x.Contains(dataGridView1.SelectedRows[0].Cells["Image"].Value.ToString())));    //Remove the old value
                        flagged_images.Add(dataGridView1.SelectedRows[0].Cells["Image"].Value.ToString() + "=" + FlagComboBox.SelectedItem.ToString());
                    }
                    else
                    {
                        flagged_images.Add(dataGridView1.SelectedRows[0].Cells["Image"].Value.ToString() + "=" + FlagComboBox.SelectedItem.ToString());
                    }

                    //Update dataGridView
                    dataGridView1.SelectedRows[0].Cells["Flag"].Value = FlagComboBox.SelectedItem.ToString();
                    dataGridView1.SelectedRows[0].Cells["Flag"].Style.ForeColor = StringToColor(FlagComboBox.SelectedItem.ToString());
                    dataGridView1.SelectedRows[0].Cells["Flag"].Style.BackColor = StringToColor(FlagComboBox.SelectedItem.ToString());
                    dataGridView1.SelectedRows[0].Cells["Flag"].Style.SelectionBackColor = StringToColor(FlagComboBox.SelectedItem.ToString());
                    dataGridView1.SelectedRows[0].Cells["Flag"].Style.SelectionForeColor = StringToColor(FlagComboBox.SelectedItem.ToString());

                    //Update the checkbox.ini
                    UpdateFlagged(selectResultsFolder, flagged_images.ToArray());
                }

            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //------------------------------------------------Down Arrow Pressed-------------------------------------------------
        //===================================================================================================================

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Space)
                {
                    btnMarkAsViewed_Click(sender, e);
                }

            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //-----------------------------------------------Mouse Move Handler--------------------------------------------------
        //===================================================================================================================

        //Mouse moved over pictureBox2
        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                //Update mouse positions
                mouse_x = e.X;
                mouse_y = e.Y;
                /*
                if ( pbox1_left || pbox2_left || pbox1_right || pbox2_right )
                    if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
                    {
                        int size = 90;
                        int offset = size / 2;
                        float image_ratio = ((float)pictureBox1.Image.Width / (float)pictureBox1.Image.Height);
                        int expected_height = (int)((float)pictureBox1.Width / image_ratio);
                        int height_offset = (pictureBox1.Height - expected_height) / 2;

                        path.AddEllipse(mouse_x - offset, mouse_y - offset, size, size);
                        pictureBox1.Invalidate();
                        pictureBox2.Invalidate();
                    }
                */
                //Update lbl_Info with mouse positions
                lbl_Info.Text = string.Format("( {0}, {1} )", mouse_x, mouse_y);
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }

        }

        //===================================================================================================================
        //----------------------------------------------Mouse Click Handlers--------------------------------------------------
        //===================================================================================================================

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                //Update mouse positions
                mouse_x = e.X;
                mouse_y = e.Y;

                int size = 90;
                int offset = size / 2;
                float image_ratio = ((float)pictureBox1.Image.Width / (float)pictureBox1.Image.Height);
                int expected_height = (int)((float)pictureBox1.Width / image_ratio);
                int height_offset = (pictureBox1.Height - expected_height) / 2;             

                if (e.Button == MouseButtons.Left)
                {
                    pbox1_left = true;
                }

                if (e.Button == MouseButtons.Right)
                {
                    pbox1_right = true;
                    path.AddEllipse(mouse_x - offset, mouse_y - offset, size, size);
                }

                pictureBox1.Invalidate();
                pictureBox2.Invalidate();
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //----------------------------------------------Mouse Click Handlers--------------------------------------------------
        //===================================================================================================================

        private void pictureBox2_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                //Update mouse positions
                mouse_x = e.X;
                mouse_y = e.Y;

                int size = 90;
                int offset = size / 2;
                float image_ratio = ((float)pictureBox1.Image.Width / (float)pictureBox1.Image.Height);
                int expected_height = (int)((float)pictureBox1.Width / image_ratio);
                int height_offset = (pictureBox1.Height - expected_height) / 2;

                if (e.Button == MouseButtons.Left)
                {
                    pbox2_left = true;
                }

                if (e.Button == MouseButtons.Right)
                {
                    pbox2_right = true;
                    path.AddEllipse(mouse_x - offset, mouse_y - offset, size, size);
                }

                pictureBox1.Invalidate();
                pictureBox2.Invalidate();
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //-------------------------------------------PictureBox OnPaint Handlers---------------------------------------------
        //===================================================================================================================

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                if (pictureBox1.Image != null)
                {
                    int size = 90;
                    int offset = size / 2;
                    float image_ratio = ((float)pictureBox1.Image.Width / (float)pictureBox1.Image.Height);
                    int expected_height = (int)((float)pictureBox1.Width / image_ratio);
                    int height_offset = (pictureBox1.Height - expected_height) / 2;

                    if (pbox1_left)
                    {
                        e.Graphics.FillEllipse(brush, mouse_x - offset, mouse_y - offset, size, size);
                    }

                    if (pbox1_right)
                    {
                        e.Graphics.FillPath(brush, path);
                    }

                    if (pbox2_left)
                    {
                        GraphicsPath temp_path = new GraphicsPath();
                        temp_path.AddEllipse(mouse_x - offset, mouse_y - offset, size, size);

                        e.Graphics.ExcludeClip(new Region(temp_path));
                        e.Graphics.FillRectangle(brush, 0, height_offset, pictureBox1.Width, expected_height);
                    }

                    if (pbox2_right)
                    {
                        e.Graphics.SetClip(path, CombineMode.Exclude);
                        e.Graphics.FillRectangle(brush, 0, height_offset, pictureBox1.Width, expected_height);
                    }
                }
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }

        }

        //===================================================================================================================
        //-------------------------------------------PictureBox OnPaint Handlers---------------------------------------------
        //===================================================================================================================

        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                if (pictureBox2.Image != null)
                {
                    int size = 90;
                    int offset = size / 2;
                    float image_ratio = ((float)pictureBox1.Image.Width / (float)pictureBox1.Image.Height);
                    int expected_height = (int)((float)pictureBox1.Width / image_ratio);
                    int height_offset = (pictureBox1.Height - expected_height) / 2;

                    if (pbox2_left)
                    {
                        e.Graphics.FillEllipse(brush, mouse_x - offset, mouse_y - offset, size, size);
                    }

                    if (pbox2_right)
                    {
                        e.Graphics.FillPath(brush, path);
                    }

                    if (pbox1_left)
                    {
                        GraphicsPath temp_path = new GraphicsPath();
                        temp_path.AddEllipse(mouse_x - offset, mouse_y - offset, size, size);

                        e.Graphics.ExcludeClip(new Region(temp_path));
                        e.Graphics.FillRectangle(brush, 0, height_offset, pictureBox1.Width, expected_height);
                    }

                    if (pbox1_right)
                    {
                        e.Graphics.SetClip(path, CombineMode.Exclude);
                        e.Graphics.FillRectangle(brush, 0, height_offset, pictureBox1.Width, expected_height);
                    }

                    //PictureBox2 is the last to be drawn, therefore the click bools can be reset
                    pbox1_left = false;
                    pbox2_left = false;
                    pbox1_right = false;
                    pbox2_right = false;

                }
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //------------------------------------------------Mouse Click Up-----------------------------------------------------
        //===================================================================================================================

        private void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                //Clear screen of drawn markers
                if (e.Button == MouseButtons.Left)
                {
                    pictureBox1.Refresh();
                    pictureBox2.Refresh();
                    path.Reset();
                }
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //-------------------------------------------Select Algorithms Clicked-----------------------------------------------
        //===================================================================================================================

        private void selectAlgorithmsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                AlgorithmsForm af = new AlgorithmsForm();
                af.ShowDialog();
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //-------------------------------------------------Image Flagged-----------------------------------------------------
        //===================================================================================================================

        private void FlagComboBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            try
            {
                MarkAsFlagged();
                FlagComboBox.SelectedIndex = -1;
                dataGridView1.Update();
                dataGridView1.Focus();
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //--------------------------------------------------Clear Flag-------------------------------------------------------
        //===================================================================================================================

        private void btnClearFlag_Click(object sender, EventArgs e)
        {
            try
            {
                if (flagged_images.Any<string>((string x) => x.Split('=')[0].Contains(dataGridView1.SelectedRows[0].Cells["Image"].Value.ToString())))
                {
                    flagged_images.Remove(flagged_images.Find((string x) => x.Contains(dataGridView1.SelectedRows[0].Cells["Image"].Value.ToString())));    //Remove the old value
                }

                //Update dataGridView
                dataGridView1.SelectedRows[0].Cells["Flag"].Value = "";
                dataGridView1.SelectedRows[0].Cells["Flag"].Style.ForeColor = Color.White;
                dataGridView1.SelectedRows[0].Cells["Flag"].Style.BackColor = Color.White;
                dataGridView1.SelectedRows[0].Cells["Flag"].Style.SelectionBackColor = Color.FromKnownColor(KnownColor.Highlight);
                dataGridView1.SelectedRows[0].Cells["Flag"].Style.SelectionForeColor = Color.FromKnownColor(KnownColor.Highlight);

                //Update the checkbox.ini
                UpdateFlagged(selectResultsFolder, flagged_images.ToArray());

                dataGridView1.Update();
                dataGridView1.Focus();
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //-------------------------------------------------------------------------------------------------------------------
        //===================================================================================================================

        private void saveImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {

            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }

        //===================================================================================================================
        //-------------------------------------------------------------------------------------------------------------------
        //===================================================================================================================

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                AboutBox about = new AboutBox();
                about.ShowDialog();
            }
            catch (Exception err)
            {
                elog.Log(err.TargetSite.ToString(), err.Message);
                //MessageBox.Show(err.Message);
            }
        }





        //===================================================================================================================
        //-------------------------------------------------------------------------------------------------------------------
        //===================================================================================================================

        //===================================================================================================================
        //-------------------------------------------------------------------------------------------------------------------
        //===================================================================================================================

    }
}
