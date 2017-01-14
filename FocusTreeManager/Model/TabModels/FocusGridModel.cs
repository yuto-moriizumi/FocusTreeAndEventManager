﻿using FocusTreeManager.DataContract;
using FocusTreeManager.ViewModel;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FocusTreeManager.Model
{
    public class FocusGridModel : ObservableObject
    {
        const int MIN_ROW_COUNT = 7;
        const int MIN_COLUMN_COUNT = 20;

        private ObservableCollection<CanvasLine> canvasLines;

        private FocusModel selectedFocus;

        public CanvasLine selectedLine { get; set; }

        private int rowCount;

        private int columnCount;

        private Guid ID;

        public RelayCommand<object> AddFocusCommand { get; private set; }

        public RelayCommand<object> RightClickCommand { get; private set; }

        public RelayCommand<object> HoverCommand { get; private set; }

        public bool isShown { get; set; }

        public int RowCount
        {
            get
            {
                return rowCount;
            }
            set
            {
                rowCount = value;
                RaisePropertyChanged("RowCount");
            }
        }

        public int ColumnCount
        {
            get
            {
                return columnCount;
            }
            set
            {
                columnCount = value;
                RaisePropertyChanged("ColumnCount");
            }
        }

        public Guid UniqueID
        {
            get
            {
                return ID;
            }
        }

        public string Filename
        {
            get
            {
                var element = Project.Instance.getSpecificFociList(ID);
                return element != null ? element.ContainerID : null;
            }
        }

        public string TAG
        {
            get
            {
                var element = Project.Instance.getSpecificFociList(ID);
                return element != null ? element.TAG : null;
            }
            set
            {
                var element = Project.Instance.getSpecificFociList(ID);
                if (element != null)
                {
                    element.TAG = value;
                }
            }
        }

        public ObservableCollection<FocusModel> FociList { get; private set; }

        public ObservableCollection<CanvasLine> CanvasLines
        {
            get
            {
                return canvasLines;
            }
            set
            {
                canvasLines = value;
                RaisePropertyChanged("CanvasLines");
            }
        }

        public FocusGridModel(Guid ID)
        {
            FociList = new ObservableCollection<FocusModel>();
            //Min Row & column Count
            RowCount = MIN_ROW_COUNT;
            ColumnCount = MIN_COLUMN_COUNT;
            this.ID = ID;
            RefreshFociList();
            canvasLines = new ObservableCollection<CanvasLine>();
            //Commands
            AddFocusCommand = new RelayCommand<object>(AddFocus);
            RightClickCommand = new RelayCommand<object>(RightClick);
            HoverCommand = new RelayCommand<object>(Hover);
            //Messenger
            Messenger.Default.Register<NotificationMessage>(this, NotificationMessageReceived);
        }

        private void RefreshFociList()
        {
            var element = Project.Instance.getSpecificFociList(ID);
            List<FocusModel> Value = element != null ? element.getFocusModelList() : null;
            if (Value != null)
            {
                FociList.Clear();
                foreach (FocusModel item in Value)
                {
                    FociList.Add(item);
                }
            }
            RaisePropertyChanged(() => FociList);
        }

        internal void ChangePosition(object draggedElement, Point currentPoint)
        {
            if (!(draggedElement is FocusModel))
            {
                return;
            }
            else
            {
                int X = (int)Math.Floor(currentPoint.X / 89);
                int Y = (int)Math.Floor(currentPoint.Y / 140);
                ((FocusModel)draggedElement).X = X;
                ((FocusModel)draggedElement).Y = Y;
                EditGridDefinition();
                DrawOnCanvas();
            }
        }

        public void EditGridDefinition()
        {
            if (!FociList.Any())
            {
                return;
            }
            FocusModel biggestY = FociList.Aggregate((i1, i2) => i1.Y > i2.Y ? i1 : i2);
            FocusModel biggestX = FociList.Aggregate((i1, i2) => i1.X > i2.X ? i1 : i2);
            RowCount = biggestY.Y >= RowCount ? biggestY.Y + 1 : RowCount;
            ColumnCount = biggestX.X >= ColumnCount ? biggestX.X + 1 : ColumnCount;
        }

        public void AddGridCells(int RowsToAdd, int ColumnsToAddt)
        {
            RowCount += RowsToAdd;
            ColumnCount += ColumnsToAddt;
        }

        public void AddFocus(object sender)
        {
            System.Windows.Application.Current.Properties["Mode"] = "Add";
            Messenger.Default.Send(new NotificationMessage(sender, "ShowAddFocus"));
        }

        public void RightClick(object sender)
        {
            System.Windows.Point Position = Mouse.GetPosition((Grid)sender);
            List<CanvasLine> clickedElements = CanvasLines.Where((line) =>
                                            inRange((int)line.X1, (int)line.X2, (int)Position.X) &&
                                            inRange((int)line.Y1, (int)line.Y2, (int)Position.Y)).ToList();
            if (clickedElements.Any())
            { 
                foreach (CanvasLine line in clickedElements)
                {
                    line.InternalSet.DeleteSetRelations();
                }
                CanvasLines = new ObservableCollection<CanvasLine>(CanvasLines.Except(clickedElements).ToList());
                DrawOnCanvas();
            }
        }

        public void Hover(object sender)
        {
            System.Windows.Point Position = Mouse.GetPosition((Grid)sender);
            List<CanvasLine> clickedElements = CanvasLines.Where((line) =>
                                            inRange((int)line.X1, (int)line.X2, (int)Position.X) &&
                                            inRange((int)line.Y1, (int)line.Y2, (int)Position.Y)).ToList();
            if (clickedElements.Any())
            {
                selectedLine = clickedElements.FirstOrDefault();
                Messenger.Default.Send(new NotificationMessage("DrawOnCanvas"));
            }
            else
            {
                if (selectedLine != null)
                {
                    selectedLine = null;
                    Messenger.Default.Send(new NotificationMessage("DrawOnCanvas"));
                }
            }
        }

        public bool inRange(int Range1, int Range2, int Value)
        {
            int smallest = Math.Min(Range1, Range2);
            int highest = Math.Max(Range1, Range2);
            return ((smallest - 2 <= Value && Value <= highest - 2) ||
                    (smallest - 1 <= Value && Value <= highest - 1) ||
                    (smallest <= Value && Value <= highest) ||
                    (smallest + 1 <= Value && Value <= highest + 1) ||
                    (smallest + 2 <= Value && Value <= highest + 2));
        }

        public void RedrawGrid()
        {
            EditGridDefinition();
            DrawOnCanvas();
        }

        private void NotificationMessageReceived(NotificationMessage msg)
        {
            if (this.Filename == null)
            {
                return;
            }
            //Always manage container renamed
            if (msg.Notification == "ContainerRenamed")
            {
                RaisePropertyChanged(() => Filename);
            }
            if (!this.isShown)
            {
                //is not shown, do not manage
                return;
            }
            FocusModel Model = msg.Sender as FocusModel;
            switch (msg.Notification)
            {
                case "HideAddFocus":
                    System.Windows.Application.Current.Properties["Mode"] = null;
                    AddFocusViewModel viewModel = msg.Sender as AddFocusViewModel;
                    addFocusToList(viewModel.Focus);
                    DrawOnCanvas();
                    break;
                case "HideEditFocus":
                    System.Windows.Application.Current.Properties["Mode"] = null;
                    EditGridDefinition();
                    DrawOnCanvas();
                    break;
                case "DeleteFocus":
                    DeleteFocus(Model);
                    break;
                case "AddFocusMutually":
                    System.Windows.Application.Current.Properties["Mode"] = "Mutually";
                    selectedFocus = Model;
                    Model.IsSelected = true;
                    break;
                case "FinishAddFocusMutually":
                    if (selectedFocus != null && selectedFocus != Model &&
                        FociList.Where((f) => f == Model).Any())
                    {
                        System.Windows.Application.Current.Properties["Mode"] = null;
                        selectedFocus.IsSelected = false;
                        var tempo = new MutuallyExclusiveSet(selectedFocus.DataContract, Model.DataContract);
                        selectedFocus.DataContract.MutualyExclusive.Add(tempo);
                        Model.DataContract.MutualyExclusive.Add(tempo);
                        RaisePropertyChanged(() => FociList);
                        DrawOnCanvas();
                    }
                    break;
                case "AddFocusPrerequisite":
                    System.Windows.Application.Current.Properties["Mode"] = "Prerequisite";
                    selectedFocus = Model;
                    Model.IsSelected = true;
                    break;
                case "FinishAddFocusPrerequisite":
                    if (selectedFocus != null && selectedFocus != Model &&
                        FociList.Where((f) => f == Model).Any())
                    {
                        System.Windows.Application.Current.Properties["Mode"] = null;
                        string Type = (string)System.Windows.Application.Current.Properties["ModeParam"];
                        System.Windows.Application.Current.Properties["ModeParam"] = null;
                        selectedFocus.IsSelected = false;
                        if (Type == "Required")
                        {
                            //Create new set
                            PrerequisitesSet set = new PrerequisitesSet(selectedFocus.DataContract);
                            set.FociList.Add(Model.DataContract);
                            selectedFocus.DataContract.Prerequisite.Add(set);
                        }
                        else
                        {
                            //Create new set if no exist
                            if (!selectedFocus.Prerequisite.Any())
                            {
                                PrerequisitesSet set = new PrerequisitesSet(selectedFocus.DataContract);
                                selectedFocus.DataContract.Prerequisite.Add(set);
                            }
                            //Add Model to last Set
                            selectedFocus.DataContract.Prerequisite.Last().FociList.Add(Model.DataContract);
                        }
                        RaisePropertyChanged(() => FociList);
                        DrawOnCanvas();
                    }
                    break;
            }
        }

        public void UpdateFocus(FocusModel sender)
        {
            if (this.isShown)
            {
                EditGridDefinition();
                DrawOnCanvas();
            }
        }

        private void DeleteFocus(FocusModel Model)
        {
            //Kill the set that might have this focus as parent
            foreach (FocusModel focus in FociList)
            {
                foreach (PrerequisitesSetModel set in focus.Prerequisite.ToList())
                {
                    if (set.FociList.Contains(Model))
                    {
                        set.DataContract.DeleteSetRelations();
                    }
                }
                foreach (MutuallyExclusiveSetModel set in focus.MutualyExclusive.ToList())
                {
                    if (set.Focus2 == Model || set.Focus1 == Model)
                    {
                        set.DataContract.DeleteSetRelations();
                    }
                }
            }
            //Kill the focus in the project
            Project.Instance.getSpecificFociList(ID).FociList.Remove(Model.DataContract);
            FociList.Remove(Model);
            EditGridDefinition();
            DrawOnCanvas();
        }

        public void addFocusToList(FocusModel FocusToAdd)
        {
            Project.Instance.getSpecificFociList(ID).FociList.Add(FocusToAdd.DataContract);
            FociList.Add(FocusToAdd);
            RowCount = FocusToAdd.Y >= RowCount ? FocusToAdd.Y + 1 : RowCount;
            ColumnCount = FocusToAdd.X >= ColumnCount ? FocusToAdd.X + 1 : ColumnCount;
            DrawOnCanvas();
        }

        const double PRE_LINE_HEIGHT = 20;

        public void DrawOnCanvas()
        {
            if (FociList == null)
            {
                return;
            }
            CanvasLines.Clear();
            foreach (FocusModel focus in FociList)
            {
                //Draw Prerequisites
                foreach (PrerequisitesSetModel set in focus.Prerequisite)
                {
                    //Draw line from top of first Focus 
                    CanvasLine newline = new CanvasLine(
                        set.Focus.FocusTop.X,
                        set.Focus.FocusTop.Y,
                        set.Focus.FocusTop.X,
                        set.Focus.FocusTop.Y - PRE_LINE_HEIGHT,
                        System.Windows.Media.Brushes.Teal, set.isRequired(), set.DataContract);
                    CanvasLines.Add(newline);
                    foreach (FocusModel Prerequisite in set.FociList.OfType<FocusModel>())
                    {
                        //Draw horizontal lines to prerequisite pos
                        newline = new CanvasLine(
                            set.Focus.FocusTop.X,
                            set.Focus.FocusTop.Y - PRE_LINE_HEIGHT,
                            Prerequisite.FocusBottom.X,
                            set.Focus.FocusTop.Y - PRE_LINE_HEIGHT,
                            System.Windows.Media.Brushes.Teal, set.isRequired(), set.DataContract);
                        CanvasLines.Add(newline);
                        //Draw line to prerequisite bottom
                        newline = new CanvasLine(
                            Prerequisite.FocusBottom.X,
                            set.Focus.FocusTop.Y - PRE_LINE_HEIGHT,
                            Prerequisite.FocusBottom.X,
                            Prerequisite.FocusBottom.Y,
                            System.Windows.Media.Brushes.Teal, set.isRequired(), set.DataContract);
                        CanvasLines.Add(newline);
                    }
                }
                //Draw Mutually exclusives
                foreach (MutuallyExclusiveSetModel set in focus.MutualyExclusive)
                {
                    CanvasLine newline = new CanvasLine(
                        set.Focus1.FocusRight.X,
                        set.Focus1.FocusRight.Y,
                        set.Focus2.FocusLeft.X,
                        set.Focus2.FocusLeft.Y,
                        System.Windows.Media.Brushes.Red, false, set.DataContract);
                    if (!CanvasLines.Where((line) => (line.X1 == newline.X1 &&
                                                    line.X2 == newline.X2 &&
                                                    line.Y1 == newline.Y1 &&
                                                    line.Y2 == newline.Y2)).Any())
                    {
                        CanvasLines.Add(newline);
                    }
                }
            }
            RaisePropertyChanged(() => CanvasLines);
            Messenger.Default.Send(new NotificationMessage("DrawOnCanvas"));
        }
    }
}