﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Управление_заказами.Models.Core;
using Управление_заказами.Models.Core.Abstractions;
using Управление_заказами.Models.DataBase;
using Управление_заказами.Views;

namespace Управление_заказами.ViewModels
{
    class EditOrderViewModel : BaseViewModel
    {
        private readonly IEquipmentInfo EquipmentInfo = new EquipmentInfo();
        private readonly IOrderManager OrderManager = new OrderManager();

        public Order OldOrder { get; set; }

        public EditOrderViewModel()
        {
            AddEquipmentCommand = new Command(AddEquipmentToOrder,CanAddEquipmentToOrder);
            EditOrderCommand = new Command(UpdateOrder);
            RemoveEquipmentCommand = new Command(RevoveEquipment);
            LoadEquipments();
        }

        async void LoadEquipments()
        {
            EnableProgressBar();
            Equipments = await EquipmentInfo.GetEquipmentsAsync();
            DisableProgressBar();
        }
        List<EquipmentInStock> equipments;
        public List<EquipmentInStock> Equipments
        {
            get => equipments;
            set
            {
                equipments = value;
                OnePropertyChanged();
                Categoryes = (from equipment in Equipments
                              select equipment.Category).Distinct().ToList();
            }
        }

        List<string> categoryes;
        public List<string> Categoryes
        {
            get => categoryes;
            set
            {
                categoryes = value;
                OnePropertyChanged();
            }
        }

        string selectedCategory;
        public string SelectedCategory
        {
            get => selectedCategory;
            set
            {
                selectedCategory = value;
                SelectedEquipments = (from equipments in Equipments
                                      where equipments.Category == value
                                      select equipments.Name).ToList();
                OnePropertyChanged();
            }
        }

        List<string> selectedEquipments;
        public List<string> SelectedEquipments
        {
            get => selectedEquipments;
            set
            {
                selectedEquipments = value;
                OnePropertyChanged();
            }
        }

        string selectedEquipment;
        public string SelectedEquipment
        {
            get => selectedEquipment;
            set
            {
                selectedEquipment = value;
                SelectedImage = (from eq in Equipments
                                 where eq.Name == value
                                 select eq.ImageUrl).FirstOrDefault();
                AddEquipmentCommand.OneExecuteChaneged();
                OnePropertyChanged();
            }
        }


        private int count = 1;
        public int Count
        {
            get => count;
            set
            {
                count = value;
                OnePropertyChanged();
                if (SelectedEquipmentForOrder != null && SelectedEquipment == SelectedEquipmentForOrder.Name && SelectedEquipmentForOrder.Count != value)
                {
                    SelectedEquipmentForOrder.Count = value;
                    ICollectionView view = CollectionViewSource.GetDefaultView(SelectedEquipmentsForOrder);
                    view.Refresh();
                }
            }
        }

        public DateTime StartDate { get; set; } 

        public DateTime EndDate { get; set; } 

        string selectedImage;
        public string SelectedImage
        {
            get => selectedImage;
            set
            {
                selectedImage = value;
                OnePropertyChanged();
            }
        }

        public ObservableCollection<EquipmentInStock> SelectedEquipmentsForOrder { get; set; } = new ObservableCollection<EquipmentInStock>();

        private EquipmentInStock selectedEquipmentForOrder;
        public EquipmentInStock SelectedEquipmentForOrder
        {
            get => selectedEquipmentForOrder;
            set
            {
                selectedEquipmentForOrder = value;
                OnePropertyChanged();

                if (value != null)
                {
                    Count = value.Count;
                    SelectedCategory = value.Category;
                    SelectedEquipment = value.Name;

                }
            }
        }

        public string Adress { get; set; }

        public string MobilePhone { get; set; }

        public string Note { get; set; }

        public string CustomerName { get; set; }

        public string SelectetDeliveryType { get; set; }

        public int SelectedDeliveryIndex { get; set; }


        private void RevoveEquipment(object obj)
        {
            if (SelectedEquipmentForOrder != null)
            {
                SelectedEquipmentsForOrder.Remove(SelectedEquipmentForOrder);
                AddEquipmentCommand.OneExecuteChaneged();
            }
        }

        private async void UpdateOrder(object obj)
        {
            if (EndDate < StartDate)
            {
                MessageBox.Show("Дата возврата не может быть ранее даты создания", "", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var equipmentsForOrder = new List<EquipmentFromOrder>();
            foreach (var equipmentInStock in SelectedEquipmentsForOrder)
            {
                equipmentsForOrder.Add(new EquipmentFromOrder()
                {
                    Category = equipmentInStock.Category,
                    Count = equipmentInStock.Count,
                    Name = equipmentInStock.Name,
                    StartDate = StartDate.AddSeconds(-StartDate.Second),
                    EndDate = EndDate.AddSeconds(-EndDate.Second)
                });
            }
            EnableProgressBar();
            var missingEquipments = await EquipmentInfo.GetMissingEquipments(SelectedEquipmentsForOrder.ToList(),
                StartDate.AddSeconds(-StartDate.Second),
                EndDate.AddSeconds(-EndDate.Second));
            if (missingEquipments.Count==0)
            {
                await OrderManager.UpdateOrderAsync(OldOrder.Id, new Order()
                {
                    Adress = SelectetDeliveryType.Contains("Указать адрес") ? this.Adress : "Самовывоз",
                    CustomerName = CustomerName,
                    MobilePhone = MobilePhone,
                    Manager = AppSettings.CurrentUserName,
                    CreateDate = StartDate.AddSeconds(-StartDate.Second),
                    ReturnDate = EndDate.AddSeconds(-EndDate.Second),
                    Note = Note,
                    Status = OrderStatus.Open,
                    Equipments = equipmentsForOrder,
                    GoogleCalendarColorId = AppSettings.GoogleCalendarColorId
                });
                (obj as Window).Close();
                MessageBox.Show("Заказ успешно обновлено", "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var missingEquiomentWindow = new MissingEquipmentWindow()
                {
                    DataContext = new MissingEquipmentViewModel()
                    {
                        Equioments = missingEquipments,
                    },
                    Title = "Невозможно отредактировать заказ. Не хватает оборудования."
                };
                missingEquiomentWindow.ShowDialog();
            }
           
            DisableProgressBar();
        }

        private void AddEquipmentToOrder(object obj)
        {
            SelectedEquipmentsForOrder.Add(new EquipmentInStock()
            {
                Category = SelectedCategory,
                Name = SelectedEquipment,
                Count = Count,
                ImageUrl = SelectedImage,
            });
        }

        bool CanAddEquipmentToOrder(object parametr)
        {
            if (string.IsNullOrWhiteSpace(SelectedEquipment))
            {
                return false;
            }
            foreach (var item in SelectedEquipmentsForOrder)
            {
                if (item.Name == SelectedEquipment)
                {
                    return false;
                }
            }

            return true;
        }

        public ICommand RemoveEquipmentCommand { get; set; }

        public ICommand EditOrderCommand { get; set; }

        public Command AddEquipmentCommand { get; set; }

    }
}
