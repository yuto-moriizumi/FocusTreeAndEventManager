using FocusTreeManager.CodeStructures;
using FocusTreeManager.ViewModel;
using FocusTreeManager.Views;
using GalaSoft.MvvmLight;
using System;
using FocusTreeManager.Model.TabModels;
using MonitoredUndo;

namespace FocusTreeManager.Model
{
    public class EventDescriptionModel : ObservableObject
    {
        public string Text
        {
            get
            {
                string Id;
                try
                {
                    Id = InternalScript.FindValue("text").Parse();
                }
                catch (Exception)
                {
                    Id = "eventid.d_descriptioname";
                }

                LocalisationModel locales = (new ViewModelLocator()).Main.Project.DefaultLocale;
                string translation = locales?.translateKey(Id);
                return translation ?? Id;
            }

        }

        private Script internalScript;

        public Script InternalScript
        {
            get
            {
                return internalScript;
            }
            set
            {
                if (value == internalScript)
                {
                    return;
                }
                internalScript = value;
                RaisePropertyChanged(() => InternalScript);
                RaisePropertyChanged(() => Text);
            }
        }

        public void EditDescScript()
        {
            ScripterViewModel ViewModel = new ViewModelLocator().Scripter;
            ViewModel.ScriptType = ScripterType.EventDescription;
            ViewModel.ManagedScript = internalScript;
            EditScript dialog = new EditScript();
            dialog.ShowDialog();
            internalScript = ViewModel.ManagedScript;

            //frok add updated loc text to data grid when modified description script
            RaisePropertyChanged(() => InternalScript);
            RaisePropertyChanged(() => Text);

        }

        //fork add ReloadDescScript method
        public void ReloadDescScript()
        {
            ScripterViewModel ViewModel = new ViewModelLocator().Scripter;
            ViewModel.ScriptType = ScripterType.EventDescription;
            ViewModel.ManagedScript = internalScript;
            EditScript dialog = new EditScript();
            //dialog.ShowDialog();
            internalScript = ViewModel.ManagedScript;

            //test clear
            //Properties.Settings.Default.Reset();
            //Properties.Settings.Default.Save();

            //frok add updated loc text to data grid when clicked button
            RaisePropertyChanged(() => InternalScript);
            RaisePropertyChanged(() => Text);

        }

        public void setDefaults()
        {
            internalScript = new Script();
            internalScript.Analyse("text = namespace.count.d.desc_id\ntrigger = { }");
        }

    }
}
