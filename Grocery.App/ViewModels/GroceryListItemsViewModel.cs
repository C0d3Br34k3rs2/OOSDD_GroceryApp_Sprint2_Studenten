using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grocery.App.Views;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;
using System.Collections.ObjectModel;

namespace Grocery.App.ViewModels
{
    [QueryProperty(nameof(GroceryList), nameof(GroceryList))]
    public partial class GroceryListItemsViewModel : BaseViewModel
    {
        private readonly IGroceryListItemsService _groceryListItemsService;
        private readonly IProductService _productService;
        public ObservableCollection<GroceryListItem> MyGroceryListItems { get; set; } = [];
        public ObservableCollection<Product> AvailableProducts { get; set; } = [];

        [ObservableProperty]
        GroceryList groceryList = new(0, "None", DateOnly.MinValue, "", 0);

        public GroceryListItemsViewModel(IGroceryListItemsService groceryListItemsService, IProductService productService)
        {
            _groceryListItemsService = groceryListItemsService;
            _productService = productService;
            Load(groceryList.Id);
        }

        private void Load(int id)
        {
            MyGroceryListItems.Clear();
            foreach (var item in _groceryListItemsService.GetAllOnGroceryListId(id)) MyGroceryListItems.Add(item);
            GetAvailableProducts();
        }

        private void GetAvailableProducts()
        {
            //Maak de lijst AvailableProducts leeg
            AvailableProducts.Clear();
            
            //Haal de lijst met producten op
            var allProducts = _productService.GetAll();
            
            //Controleer voor elk product in producten lijst
            foreach (var product in allProducts)
            {
                //Wanneer de product niet in de AvailableProducts lijst staat
                //En vooraad boven de 0 is
                //Zet het in de AvailableProducts lijst     
                if (!AvailableProducts.Contains(product) && product.Stock > 0)
                {
                    AvailableProducts.Add(product);
                }
            }
            _productService.GetAll();
        }

        partial void OnGroceryListChanged(GroceryList value)
        {
            Load(value.Id);
        }

        [RelayCommand]
        public async Task ChangeColor()
        {
            Dictionary<string, object> paramater = new() { { nameof(GroceryList), GroceryList } };
            await Shell.Current.GoToAsync($"{nameof(ChangeColorView)}?Name={GroceryList.Name}", true, paramater);
        }
        [RelayCommand]
        public void AddProduct(Product product)
        {
            //Controleer of het product bestaat
            if (product == null) return;
    
            //Controleer of het product al op de boodschappenlijst staat
            var existingItem = MyGroceryListItems.FirstOrDefault(item => item.ProductId == product.Id);
            
            //Maak een GroceryListItem met Id 0 en vul de juiste productid en grocerylistid
            if (existingItem != null)
            {
                //Product staat al op de lijst, verhoog de hoeveelheid
                existingItem.Amount += 1;
                _groceryListItemsService.Update(existingItem);
            }
            else
            {
                //Product bestaat nog niet in de lijst, maak nieuwe item aan
                var newGroceryListItem = new GroceryListItem(0, GroceryList.Id, product.Id, 1);
                newGroceryListItem.Product = product;
                
                //Voeg het GroceryListItem toe aan de dataset middels de _groceryListItemsService
                _groceryListItemsService.Add(newGroceryListItem);
            }
            
            //Werk de voorraad (Stock) van het product bij en zorg dat deze wordt vastgelegd (middels _productService)
            product.Stock -= 1;
            _productService.Update(product);
            
            //Werk de lijst AvailableProducts bij, want dit product is niet meer beschikbaar
            if (product.Stock == 0)
            {
                AvailableProducts.Remove(product);
            }
            
            //call OnGroceryListChanged(GroceryList);
            OnGroceryListChanged(GroceryList);
        }
    }
}
