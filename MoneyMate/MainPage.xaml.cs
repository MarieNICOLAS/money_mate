using MoneyMate.Data.Context;
using MoneyMate.Helpers;

namespace MoneyMate
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            TestDatabase();
        }

        private void TestDatabase()
        {
            try
            {
                // Test simple de la base de données
                bool dbOk = DatabaseTestHelper.TestDatabaseConfiguration();
                CounterBtn.Text = dbOk ? "✅ DB OK" : "❌ Erreur DB";

                System.Diagnostics.Debug.WriteLine(dbOk ? "🎉 Base de données initialisée avec succès" : "❌ Erreur lors de l'initialisation de la DB");
            }
            catch (Exception ex)
            {
                CounterBtn.Text = "❌ Erreur DB";
                System.Diagnostics.Debug.WriteLine($"Erreur: {ex.Message}");
            }
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }
}
