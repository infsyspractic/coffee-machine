using System;
using System.Collections.Generic;
using System.Linq;

namespace CoffeeMachineApp
{

    public class Drink
    {
        public string Name { get; set; }
        public int Price { get; set; }

        public Drink(string name, int price)
        {
            Name = name;
            Price = price;
        }
    }

    public class CoffeeService
    {
        public List<Drink> Drinks { get; private set; }
        public Dictionary<int, int> CashVault { get; private set; }

        public CoffeeService()
        {
            Drinks = new List<Drink>
            {
                new Drink("Эспрессо", 100),
                new Drink("Капучино", 150),
                new Drink("Латте", 200)
            };

            CashVault = new Dictionary<int, int>
            {
                { 10, 15 },
                { 50, 10 },
                { 100, 5 },
                { 200, 5 },
                { 500, 2 }
            };
        }

        public Dictionary<int, int> CalculateChange(int changeAmount)
        {
            if (changeAmount == 0) return new Dictionary<int, int>();

            var denominations = CashVault.Keys.OrderByDescending(x => x).ToList();
            
            var allCombinations = GenerateCombinations(changeAmount, denominations);

            var validCombinations = allCombinations
                .Where(comb => comb.GroupBy(x => x)
                                   .All(g => g.Count() <= CashVault[g.Key]))
                .ToList();

            if (!validCombinations.Any()) return null;


            var bestCombination = validCombinations
                .OrderByDescending(comb => comb.Distinct().Count()) 
                .ThenBy(comb => comb.GroupBy(x => x).Select(g => g.Count()).StandardDeviation()) 
                .First();

            return bestCombination
                .GroupBy(x => x)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        private List<List<int>> GenerateCombinations(int target, List<int> subDenoms)
        {
            var results = new List<List<int>>();
            for (int i = 0; i < subDenoms.Count; i++)
            {
                int coin = subDenoms[i];
                if (coin == target)
                {
                    results.Add(new List<int> { coin });
                }
                else if (coin < target)
                {
                    var remainderCombos = GenerateCombinations(target - coin, subDenoms.Skip(i).ToList());
                    foreach (var combo in remainderCombos)
                    {
                        var newCombo = new List<int> { coin };
                        newCombo.AddRange(combo);
                        results.Add(newCombo);
                    }
                }
            }
            return results;
        }

        public void ApplyChangeFromVault(Dictionary<int, int> change)
        {
            foreach (var kvp in change)
            {
                CashVault[kvp.Key] -= kvp.Value;
            }
        }

        public void TopUpVault(int denomination, int count)
        {
            if (CashVault.ContainsKey(denomination))
            {
                CashVault[denomination] += count;
            }
        }
    }

    public static class LinqExtensions
    {
        public static double StandardDeviation(this IEnumerable<int> sequence)
        {
            var list = sequence.ToList();
            if (!list.Any()) return 0;
            double avg = list.Average();
            return Math.Sqrt(list.Average(v => Math.Pow(v - avg, 2)));
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            CoffeeService coffeeService = new CoffeeService();

            while (true)
            {
                Console.Clear();
                Console.WriteLine("☕--- КОФЕМАШИНА (LINQ EDITION) ---☕\n");
                
                Console.WriteLine("Меню напитков:");
                for (int i = 0; i < coffeeService.Drinks.Count; i++)
                {
                    Console.WriteLine($" {i + 1}. {coffeeService.Drinks[i].Name} — {coffeeService.Drinks[i].Price} ₸");
                }
                Console.WriteLine(" 0. Выйти из программы");
                Console.WriteLine(" 9. [Инкассация] Пополнить баланс кассы");

                Console.Write("\nВыберите пункт меню: ");
                if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || (choice > coffeeService.Drinks.Count && choice != 9))
                {
                    ShowError("Неверный ввод. Выберите номер из меню.");
                    continue;
                }

                if (choice == 0) break;

                if (choice == 9)
                {
                    AdminTopUp(coffeeService);
                    continue;
                }

                Drink selectedDrink = coffeeService.Drinks[choice - 1];
                Console.WriteLine($"\nВы выбрали: {selectedDrink.Name}. К оплате: {selectedDrink.Price} ₸");

                Console.Write("Внесите купюру или монету (например, 500): ");
                if (!int.TryParse(Console.ReadLine(), out int insertedMoney) || insertedMoney <= 0)
                {
                    ShowError("Сумма должна быть положительным числом.");
                    continue;
                }

                if (insertedMoney < selectedDrink.Price)
                {
                    ShowError($"Недостаточно средств! Не хватает {selectedDrink.Price - insertedMoney} ₸. Деньги возвращены.");
                    continue;
                }

                int changeAmount = insertedMoney - selectedDrink.Price;
                Console.WriteLine($"\nСдача к выдаче: {changeAmount} ₸. Расчет оптимального размена...");

                var changeResult = coffeeService.CalculateChange(changeAmount);

                if (changeResult == null)
                {
                    ShowError("Ошибка! В автомате недостаточно монет для выдачи точной сдачи. Операция отменена, заберите ваши деньги.");
                }
                else
                {

                    coffeeService.ApplyChangeFromVault(changeResult);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n[Успех] Ваш {selectedDrink.Name} готов! ☕ Приятного аппетита!");
                    
                    if (changeResult.Any())
                    {
                        Console.WriteLine("Заберите сдачу (распределено равномерно):");
                        foreach (var kvp in changeResult.OrderBy(x => x.Key))
                        {
                            Console.WriteLine($"  • Номинал {kvp.Key} ₸ -> {kvp.Value} шт.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Точный расчет! Сдача не требуется.");
                    }
                    Console.ResetColor();
                    PressAnyKey();
                }
            }
        }

        static void AdminTopUp(CoffeeService service)
        {
            Console.Clear();
            Console.WriteLine("🔧 РЕЖИМ ОБСЛУЖИВАНИЯ КАССЫ");
            Console.WriteLine("Текущий остаток монет в системе:");
            foreach (var kvp in service.CashVault.OrderBy(x => x.Key))
            {
                Console.WriteLine($"  Номинал {kvp.Key} ₸: {kvp.Value} шт.");
            }

            Console.Write("\nВведите номинал для добавления (10, 50, 100, 200, 500): ");
            if (int.TryParse(Console.ReadLine(), out int denom) && service.CashVault.ContainsKey(denom))
            {
                Console.Write("Укажите количество добавляемых монет/купюр: ");
                if (int.TryParse(Console.ReadLine(), out int count) && count > 0)
                {
                    service.TopUpVault(denom, count);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("Касса успешно обновлена!");
                    Console.ResetColor();
                }
                else ShowError("Некорректное количество.");
            }
            else ShowError("Автомат не поддерживает данный номинал.");
            PressAnyKey();
        }

        static void ShowError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ {message}");
            Console.ResetColor();
            PressAnyKey();
        }

        static void PressAnyKey()
        {
            Console.WriteLine("\nНажмите любую клавишу для возврата в главное меню...");
            Console.ReadKey();
        }
    }
}