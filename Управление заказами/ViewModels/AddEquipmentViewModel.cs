﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Управление_заказами.Models.Core;
using Управление_заказами.Models.Core.Abstractions;
using Управление_заказами.Models.DataBase;

namespace Управление_заказами.ViewModels
{
    class AddEquipmentViewModel : BaseViewModel
    {
        private readonly IEquipmentInfo EquipmentInfo = new EquipmentInfo();

        #region PrivateMembers
        private ICollectionView categoryes;
        private List<EquipmentInStock> equipments;
        private string selectedCategory;
        private string _imageUrl;

        #endregion

        public AddEquipmentViewModel()
        {
            AddEquipmentCommand = new Command(AddEquipment);
            LoadEquipments();
        }

       

        async void LoadEquipments()
        {
            EnableProgressBar();
            Equipments = await EquipmentInfo.GetEquipmentsAsync();
            DisableProgressBar();
        }

        public List<EquipmentInStock> Equipments
        {
            get => equipments;
            set
            {
                equipments = value;
                OnePropertyChanged();
                Categories = CollectionViewSource.GetDefaultView(
                    (from equipment in Equipments
                     select equipment.Category).Distinct().ToList());
            }
        }


        public ICollectionView Categories
        {
            get => categoryes;
            set
            {
                categoryes = value;
                OnePropertyChanged();
            }
        }

        public string SelectedCategory
        {
            get => selectedCategory;
            set
            {
                selectedCategory = value;
                if (Categories != null)
                    Categories.Filter = item => item.ToString().ToLower().Contains(value.ToLower());
            }
        }

        public string EquipmentName { get; set; }

        public int Count { get; set; } = 1;

        public decimal ReplacmentCost { get; set; } = 0;

        public string ImageUrl
        {
            get => _imageUrl;
            set
            {
                _imageUrl = value;
                OnePropertyChanged();
            }
        }


        public ICommand AddEquipmentCommand { get; set; }

        private async void AddEquipment(object obj)
        {
            if (string.IsNullOrWhiteSpace(SelectedCategory))
            {
                MessageBox.Show("Категорию не введено", "", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(EquipmentName))
            {
                MessageBox.Show("Название оборудования не введено", "", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            EquipmentInStock equipment = new EquipmentInStock()
            {
                Name = EquipmentName,
                Category = SelectedCategory,
                ReplacmentCost = ReplacmentCost,
                ImageUrl = ImageUrl,
                TotalCount = Count,
                Count = Count
            };

            try
            {
                EnableProgressBar();
                await EquipmentInfo.AddEquipment(equipment);
                MessageBox.Show("Оборудование успешно добавлено","",MessageBoxButton.OK,MessageBoxImage.Information);
            }
            catch(ArgumentException)
            {
                MessageBox.Show("Оборудование с таким именем уже существует в базе данных", "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DisableProgressBar();
            }
        }
    }
}
