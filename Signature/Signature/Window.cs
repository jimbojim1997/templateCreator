﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;

namespace Signature
{
    public partial class window : Form
    {
        #region globals
        private string templateFilePath;
        private string templateData;
        private string signatureSavePath;
        private string signatureSaveName;
        #endregion


        #region functions
        public window()
        {
            InitializeComponent();
        }

        private void createSignatures(string templateData, Field[][] data, string saveFilePath, string saveFileNameExpression)
        {
            //go through each signature
            foreach(Field[] fields in data)
            {
                //generate file name
                string sigName = saveFileNameExpression;
                foreach(Field field in fields)
                {
                    sigName = replaceField(sigName, field.field, field.data);
                }

                //generate signature
                string signature = templateData;
                foreach (Field field in fields)
                {
                    signature = replaceField(signature, field.field, field.data);
                }

                sigName = String.Format(@"{0}\{1}.html", saveFilePath, sigName);
                saveFile(sigName, signature);
            }
        }

        private string replaceField(string text, string fieldName, string data)
        {
            string field = String.Format("[{0}]", fieldName);
            if (String.IsNullOrEmpty(data))
            {
                data = "";
            }

            int start;
            while((start = text.IndexOf(field)) != -1)// loop through all fields
            {
                //check conditionals after [field][~CONDITIONAL]
                if (start + field.Length < text.Length)
                {
                    if (text.Substring(start + field.Length, 2) == "[~")
                    {
                        int layer = 0;
                        for (int x = start + field.Length + 1; x < text.Length; x++)
                        {
                            char current = text[x];
                            if (current == '[')
                            {
                                layer++;
                            }
                            else if (current == ']')
                            {
                                if (layer > 0)
                                {
                                    layer--;
                                }
                                else if (String.IsNullOrEmpty(data))
                                {
                                    text.Substring(start + field.Length, x - (start + field.Length));
                                    text = text.Remove(start + field.Length, x + 1 - (start + field.Length));
                                    break;
                                }
                                else
                                {
                                    text = text.Remove(x, 1);
                                    text = text.Remove(start + field.Length, 2);
                                    break;
                                }
                            }
                        }
                    }
                }

                //check conditionals before [CONDITIONAL~][field]
                if (start >= 2)
                {
                    if (text.Substring(start - 2, 2) == "~]")
                    {
                        int layer = 0;
                        for (int x = start - 2; x >= 0; x--)
                        {
                            char current = text[x];
                            if (current == ']')
                            {
                                layer++;
                            }
                            else if (current == '[')
                            {
                                if (layer > 0)
                                {
                                    layer--;
                                }
                                else if (String.IsNullOrEmpty(data))
                                {
                                    text = text.Remove(x, start - x);
                                    break;
                                }
                                else
                                {
                                    text = text.Remove(x, 1);
                                    text = text.Remove(start - 3, 2);
                                    break;
                                }
                            }
                        }
                    }
                }

                text = ReplaceFirst(text, field, data);
            }
            return text;
        }

        private Field[][] getData()
        {
            List<List<Field>> data = new List<List<Field>>();
            Field[] headers = getFields();

            foreach(DataGridViewRow row in dgvData.Rows)
            {
                List<Field> fields = new List<Field>();
                for(int i = 0; i < row.Cells.Count; i++)
                {
                    fields.Add(new Field(headers[i].name, headers[i].field, (string)row.Cells[i].Value));
                }
                data.Add(fields);
            }
            data.RemoveAt(data.Count - 1);

            Field[][] dataArray = new Field[data.Count][];
            for (int i = 0; i < dataArray.Length; i++)
            {
                dataArray[i] = data[i].ToArray();
            }

            return dataArray;
        }

        private Field[] getFields()
        {
            List<Field> fields = new List<Field>();
            
            foreach(DataGridViewRow row in dgvFields.Rows)
            {
                fields.Add(new Field((string)row.Cells[0].Value, (string)row.Cells[1].Value));
                
            }

            return fields.ToArray();
        }

        private string ReplaceFirst(string text, string oldString, string newString)
        {
            if (newString != null)
            {
                Regex reg = new Regex(Regex.Escape(oldString));
                text = reg.Replace(text, newString, 1);
            }
            return text;
        }

        private string readFile(string path)
        {
            string data;
            using (FileStream fs = new FileStream(path, FileMode.Open))
            using (StreamReader sr = new StreamReader(fs))
            {
                data = sr.ReadToEnd();
            }
            return data;
        }

        private void saveFile(string path, string data)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create))
            using (StreamWriter sr = new StreamWriter(fs))
            {
                sr.Write(data);
            }
        }
        #endregion

        #region events
        private void btnCreateSignatures_Click(object sender, EventArgs e)
        {
            signatureSaveName = tbFileNames.Text;
            if (templateData != null && signatureSavePath != null & signatureSaveName != null)
            {
                createSignatures(templateData, getData(), signatureSavePath, signatureSaveName);
            }
            else
            {
                MessageBox.Show("You are missing some fileds.\nCheck that their is a template, save location and save name set.", "Something's missing...");
            }
        }

        private void btnFieldsLoadFromTemplate_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(templateData))
            {
                MatchCollection matchs = Regex.Matches(templateData, @"\[([^~\[\]]+)\]");
                List<string> fields = new List<string>();

                foreach(Match match in matchs)
                {
                    Boolean isMatch = false;
                    string next = match.Groups[1].Value;
                    foreach(string field in fields)
                    {
                        if(next == field)
                        {
                            isMatch = true;
                            break;
                        }
                    }

                    if (!isMatch)
                    {
                        fields.Add(next);
                    }
                }

                foreach(string field in fields)
                {
                    DataGridViewRow row = (DataGridViewRow)dgvFields.Rows[0].Clone();
                    row.Cells[0].Value = field;
                    row.Cells[1].Value = field;

                    dgvFields.Rows.Add(row);
                }
            }
        }

        private void btnFieldsLoadFromFile_Click(object sender, EventArgs e)
        {

        }

        private void btnFieldsSaveToFile_Click(object sender, EventArgs e)
        {

        }

        private void btnFieldsUpdate_Click(object sender, EventArgs e)
        {
            if(dgvData.Rows.Count > 1)
            {
                if(MessageBox.Show("You have entries in the Data tab, doing this will delete this data.\nContinue?","Warning!", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    return;
                }
            }

            Field[] headers = getFields();
            dgvData.Columns.Clear();
            foreach(Field field in headers)
            {
                DataGridViewColumn col = new DataGridViewColumn(new DataGridViewTextBoxCell());
                col.Name = field.name;
                col.HeaderText = field.name;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

                dgvData.Columns.Add(col);

            }
            dgvData.Columns.RemoveAt(dgvData.Columns.Count-1);
        }

        private void btnFieldsReset_Click(object sender, EventArgs e)
        {
            if (dgvData.Rows.Count > 1)
            {
                if (MessageBox.Show("You have entries in the Data tab, doing this will delete this data.\nContinue?", "Warning!", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    return;
                }
            }
            dgvFields.Rows.Clear();
            dgvData.Columns.Clear();
        }

        private void btnFieldsMoveUp_Click(object sender, EventArgs e)
        {
            if(dgvFields.SelectedRows.Count > 0)
            {
                DataGridViewRow selected = dgvFields.SelectedRows[0];
                if (dgvFields.Rows.IndexOf(selected) > 0)
                {
                    int index = selected.Index;
                    DataGridViewRow last = dgvFields.Rows[index - 1];
                    dgvFields.Rows.Remove(last);
                    dgvFields.Rows.Insert(index, last);
                    dgvFields.ClearSelection();
                    selected.Selected = true;
                }
            }
        }

        private void btnFieldsMoveDown_Click(object sender, EventArgs e)
        {
            if(dgvFields.SelectedRows.Count > 0)
            {
                DataGridViewRow selected = dgvFields.SelectedRows[0];
                if (dgvFields.Rows.IndexOf(selected) < dgvFields.Rows.Count)
                {
                    int index = selected.Index;
                    DataGridViewRow next = dgvFields.Rows[index + 1];
                    dgvFields.Rows.Remove(next);
                    dgvFields.Rows.Insert(index, next);
                    dgvFields.ClearSelection();
                    selected.Selected = true;
                }
            }
        }

        private void btnTemplateFileLoad_Click(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Title = "Signature Template";
            fd.Filter = "HTML|*.html";
            
            if (fd.ShowDialog() == DialogResult.OK)
            {
                templateFilePath = fd.FileName;
                tbTemplateFile.Text = fd.FileName;

                templateData = readFile(fd.FileName);
                wbTemplate.Document.Body.InnerHtml = templateData;
            }
        }

        private void btnTemplateFileSave_Click(object sender, EventArgs e)
        {

        }

        private void btnSaveLocation_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fd = new FolderBrowserDialog();
            fd.ShowNewFolderButton = true;
            fd.RootFolder = Environment.SpecialFolder.Desktop;
            if (File.Exists(templateFilePath))
            {
                fd.SelectedPath = Path.GetDirectoryName(templateFilePath);
            }

            if (fd.ShowDialog() == DialogResult.OK)
            {
                signatureSavePath = fd.SelectedPath;
                tbSaveLocation.Text = signatureSavePath;
            }
        }

        private void btnFileNamesVarify_Click(object sender, EventArgs e)
        {
            Regex reg = new Regex(@"\[([\w\s]+)\]");
            MatchCollection matchs = reg.Matches(tbFileNames.Text);
            string noMatch = "";
            Field[] fields = getFields();
            
            foreach(Match match in matchs)
            {
                bool hasMatched = false;
                foreach(Field field in fields)
                {
                    if(match.Groups[1].Value == field.field)
                    {
                        hasMatched = true;
                        break;
                    }
                }
                if (!hasMatched)
                {
                    noMatch = String.Format("{0}, ", match.Value);
                }
            }


            if (String.IsNullOrEmpty(noMatch))
            {
                MessageBox.Show("Expression is OK.", "Success");
            }
            else
            {
                string message = String.Format("{0}\n{1}\n{2}", "The field(s):", noMatch, "haven't been defined.");
                MessageBox.Show(message, "Fail");
            }

        }

        private void window_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (MessageBox.Show("Any unsaved work will be lost!\nContinue?", "Warning!", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }
        #endregion

        private void window_Load(object sender, EventArgs e)
        {

        }

        
    }
}