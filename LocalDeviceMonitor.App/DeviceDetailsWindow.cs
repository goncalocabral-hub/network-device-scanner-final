using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LocalDeviceMonitor.App
{
    public class DeviceDetailsWindow : Window
    {
        private readonly AssetRepository _repo = new();
        private int _ativoId;

        public DeviceDetailsWindow(DeviceInfo device)
        {
            Width = 500;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // 🔑 Criar chave única consistente
            string uniqueKey =
                !string.IsNullOrWhiteSpace(device.MacAddress) ? device.MacAddress :
                !string.IsNullOrWhiteSpace(device.IpAddress) ? device.IpAddress :
                device.Name;

            // 🔥 Criar ou atualizar ativo
            _ativoId = _repo.UpsertAtivo(device.Name, device.Origin, uniqueKey);

            Title = device.Name;

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var stack = new StackPanel
            {
                Margin = new Thickness(12)
            };

            void AddLine(string label, string? value)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = $"{label}: {(string.IsNullOrWhiteSpace(value) ? "-" : value)}",
                    Margin = new Thickness(0, 2, 0, 4)
                });
            }

            // 🔹 Info base
            AddLine("IP", device.IpAddress);
            AddLine("MAC", device.MacAddress);
            AddLine("Fabricante", device.Manufacturer);
            AddLine("Tipo", device.DeviceType);
            AddLine("Estado", device.Status);

            // =========================
            // 🔥 EDITAR NOME
            // =========================

            stack.Children.Add(new TextBlock
            {
                Text = "Nome do Ativo:",
                Margin = new Thickness(0, 10, 0, 2)
            });

            var txtNome = new TextBox
            {
                Text = device.Name,
                Margin = new Thickness(0, 0, 0, 10)
            };

            stack.Children.Add(txtNome);

            var btnGuardar = new Button
            {
                Content = "Guardar Nome",
                Height = 30,
                Margin = new Thickness(0, 0, 0, 15)
            };

            btnGuardar.Click += (s, e) =>
            {
                var novoNome = txtNome.Text.Trim();

                if (!string.IsNullOrWhiteSpace(novoNome))
                {
                    _repo.UpdateTituloAtivo(_ativoId, novoNome);

                    Title = novoNome;

                    MessageBox.Show("Nome atualizado com sucesso!",
                        "Sucesso",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            };

            stack.Children.Add(btnGuardar);

            // =========================
            // 🔥 ATRIBUTOS
            // =========================

            stack.Children.Add(new TextBlock
            {
                Text = "Atributos:",
                Margin = new Thickness(0, 10, 0, 5)
            });

            var atributos = _repo.GetAtributos();
            var ativosAtributos = _repo.GetAtributosDoAtivo(_ativoId);

            foreach (var attr in atributos)
            {
                var cb = new CheckBox
                {
                    Content = attr.Nome,
                    IsChecked = ativosAtributos.Contains(attr.Id),
                    Margin = new Thickness(0, 2, 0, 2)
                };

                cb.Checked += (s, e) =>
                {
                    _repo.SetAtributoAtivo(_ativoId, attr.Id, true);
                };

                cb.Unchecked += (s, e) =>
                {
                    _repo.SetAtributoAtivo(_ativoId, attr.Id, false);
                };

                stack.Children.Add(cb);
            }

            scroll.Content = stack;

            // =========================
            // 🔚 FECHAR
            // =========================

            var btnFechar = new Button
            {
                Content = "Fechar",
                Height = 30,
                Margin = new Thickness(12),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            btnFechar.Click += (s, e) => Close();

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetRow(scroll, 0);
            Grid.SetRow(btnFechar, 1);

            grid.Children.Add(scroll);
            grid.Children.Add(btnFechar);

            Content = grid;
        }
    }
}