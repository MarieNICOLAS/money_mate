# Guide de Contribution - Money Mate

Bienvenue dans le projet **Money Mate** ! Ce guide vous aidera à contribuer efficacement à notre application mobile de gestion financière développée en .NET MAUI.

## 📋 Table des Matières

- [Prérequis](#prérequis)
- [Architecture du Projet](#architecture-du-projet)
- [Structure des Dossiers](#structure-des-dossiers)
- [Conventions de Codage](#conventions-de-codage)
- [Processus de Contribution](#processus-de-contribution)
- [Tests et Qualité](#tests-et-qualité)
- [Sécurité](#sécurité)
- [Documentation](#documentation)

## 🔧 Prérequis

### Environnement de Développement
- **Visual Studio 2022** avec la charge de travail .NET MAUI
- **.NET 9** SDK
- **Émulateur Android** ou dispositif physique
- **Git** pour le contrôle de version

### Connaissances Requises
- **C#** et programmation orientée objet
- **XAML** pour l'interface utilisateur
- **Architecture MVVM** (Model-View-ViewModel)
- **SQLite** pour la gestion des données locales
- **Principes SOLID**

## 🏗️ Architecture du Projet

Money Mate suit strictement l'**architecture MVVM** avec les principes suivants :

### Règles Architecturales OBLIGATOIRES

1. **Séparation Stricte des Responsabilités**
   - Les **Views** ne contiennent AUCUNE logique métier
   - Les **ViewModels** implémentent `INotifyPropertyChanged`
   - Les **Models** représentent uniquement les données
   - Les **Services** gèrent l'accès aux données et la logique métier

2. **Patterns et Interfaces**
   - Toutes les **commandes** utilisent `ICommand`
   - Les **collections** utilisent `ObservableCollection<T>`
   - Les **requêtes SQLite** sont TOUJOURS paramétrées
   - Injection de dépendances via l'interface appropriée

## 📁 Structure des Dossiers

```
MoneyMate/
├── Models/           # Entités et modèles de données
├── Views/            # Pages et interfaces XAML
├── ViewModels/       # ViewModels MVVM
├── Services/         # Services métier et accès données
├── Data/             # Contexte SQLite et repositories
├── Converters/       # Convertisseurs XAML
├── Helpers/          # Classes utilitaires
└── Resources/        # Ressources (styles, images, etc.)
```

### Détail des Modules

#### 📊 Module Authentification
**Pages :** `LoginPage`, `RegisterPage`, `ProfilePage`, `ChangePasswordPage`, `DeleteAccountPage`

**Modèle User :**
```csharp
public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }  // Hash BCrypt OBLIGATOIRE
    public string Devise { get; set; }
    public int BudgetStartDay { get; set; }
    public string Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

#### 💰 Module Dépenses
**Pages :** `ExpensesListPage`, `AddExpensePage`, `EditExpensePage`, `ExpenseDetailsPage`, `QuickAddExpensePage`

**Modèle Expense :**
```csharp
public class Expense
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime DateOperation { get; set; }
    public decimal Amount { get; set; }      // decimal(10,2)
    public int CategoryId { get; set; }
    public string Note { get; set; }
    public bool IsFixedCharge { get; set; }
}
```

#### 🏷️ Module Catégories
**Pages :** `CategoriesListPage`, `AddCategoryPage`, `EditCategoryPage`

#### 📈 Module Budgets & Seuils
**Pages :** `BudgetsOverviewPage`, `AddBudgetPage`, `EditBudgetPage`, `AlertThresholdPage`

## 💻 Conventions de Codage

### Nommage
- **Classes :** `PascalCase` (ex: `ExpenseViewModel`)
- **Méthodes :** `PascalCase` (ex: `CalculateBudgetPercentage()`)
- **Propriétés :** `PascalCase` (ex: `TotalAmount`)
- **Variables locales :** `camelCase` (ex: `budgetAmount`)
- **Constantes :** `UPPER_SNAKE_CASE` (ex: `MAX_EXPENSES_PER_PAGE`)

### Structure des Classes

#### ViewModels
```csharp
public class ExpenseViewModel : INotifyPropertyChanged
{
    private decimal _totalAmount;
    
    public decimal TotalAmount
    {
        get => _totalAmount;
        set => SetProperty(ref _totalAmount, value);
    }
    
    public ICommand AddExpenseCommand { get; }
    
    // Implémentation INotifyPropertyChanged obligatoire
    public event PropertyChangedEventHandler PropertyChanged;
}
```

#### Services
```csharp
public interface IExpenseService
{
    Task<List<Expense>> GetExpensesAsync(int userId);
    Task<bool> AddExpenseAsync(Expense expense);
}

public class ExpenseService : IExpenseService
{
    // Requêtes SQLite TOUJOURS paramétrées
    private const string GET_EXPENSES_QUERY = 
        "SELECT * FROM Expenses WHERE UserId = @userId";
}
```

### Commentaires Obligatoires
```csharp
/// <summary>
/// Calcule le pourcentage de budget consommé pour une catégorie
/// Formule : Budget consommé (%) = Dépenses / Budget
/// </summary>
/// <param name="expenses">Liste des dépenses de la catégorie</param>
/// <param name="budgetAmount">Montant du budget alloué</param>
/// <returns>Pourcentage entre 0 et 100</returns>
public decimal CalculateBudgetPercentage(decimal expenses, decimal budgetAmount)
```

## 🔄 Processus de Contribution

### 1. Préparation
```bash
# Cloner le repository
git clone [URL_DU_REPO]
cd MoneyMate

# Créer une branche feature
git checkout -b feature/nom-de-la-fonctionnalite
```

### 2. Développement
- Respecter l'architecture MVVM stricte
- Implémenter les tests unitaires
- Suivre les conventions de nommage
- Documenter le code avec des commentaires XML

### 3. Tests Avant Soumission
```bash
# Exécuter tous les tests
dotnet test

# Vérifier le build
dotnet build
```

### 4. Pull Request
- Titre descriptif : `[Module] Description courte`
- Description détaillée des changements
- Capture d'écran si modification UI
- Tests couvrant les nouveaux développements

## 🧪 Tests et Qualité

### Tests Unitaires OBLIGATOIRES
- **Couverture minimale :** 90%
- Tests pour tous les ViewModels
- Tests pour tous les Services
- Tests des calculs budgétaires

### Exemple de Test
```csharp
[Test]
public void CalculateBudgetPercentage_ShouldReturnCorrectPercentage()
{
    // Arrange
    var service = new BudgetService();
    decimal expenses = 800m;
    decimal budget = 1000m;
    
    // Act
    var result = service.CalculateBudgetPercentage(expenses, budget);
    
    // Assert
    Assert.AreEqual(80m, result);
}
```

## 🔒 Sécurité

### Exigences de Sécurité OBLIGATOIRES

1. **Mots de Passe**
   - Hash avec **BCrypt** uniquement
   - Jamais de stockage en clair

2. **Base de Données**
   - Requêtes SQLite **toujours paramétrées**
   - Pas de concaténation de chaînes

3. **Suppression de Données**
   - Suppression en cascade lors de la suppression de compte
   - Respect du RGPD

### Exemple Sécurisé
```csharp
// ✅ CORRECT - Requête paramétrée
string query = "SELECT * FROM Users WHERE Email = @email";
var parameters = new { email = userEmail };

// ❌ INCORRECT - Injection SQL possible
string badQuery = $"SELECT * FROM Users WHERE Email = '{userEmail}'";
```

## 📚 Documentation

### Code Documentation
- Commentaires XML pour toutes les méthodes publiques
- Documentation des algorithmes complexes
- Exemples d'utilisation pour les services

### Architecture Documentation
- Diagrammes de flux pour les processus métier
- Documentation des patterns utilisés
- Guide des API internes

## 🎨 Charte Graphique

### Couleurs Définies
- **Primaire :** `#6B7A8F` (Bleu-gris stabilité)
- **Secondaire :** `#F6B092` (Peach chaleureux)
- **Tertiaire :** `#E58DA3` (Rose doux alertes)
- **Fond :** `#FFF7F0` (Teinte claire chaude)
- **Cartes :** `#FFFFFF` (Blanc pur)

### Guidelines UI
- Design non-anxiogène pour contexte financier
- Cohérence sur tous les écrans
- Lisibilité optimale sur mobile

## 📞 Support et Questions

Pour toute question :
1. Vérifiez la documentation existante
2. Consultez les issues ouvertes
3. Créez une nouvelle issue avec le label `question`

## 🏆 Reconnaissance

Merci de contribuer à Money Mate ! Votre travail aide à créer une meilleure expérience de gestion financière pour tous les utilisateurs.

---

*Ce guide sera mis à jour régulièrement. Merci de le consulter avant chaque contribution.**Ce guide sera mis à jour régulièrement. Merci de le consulter avant chaque contribution.*