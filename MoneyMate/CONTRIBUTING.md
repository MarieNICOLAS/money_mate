# 💰 CONTRIBUTING.md — Money Mate

> Référence unique pour tout développement sur Money Mate (humain ou IA).
> Toute contribution doit s'y conformer **strictement**.

---

## 1. Vision Produit

Money Mate est une application mobile de gestion budgétaire personnelle.

| Caractéristique | Détail |
|----------------|--------|
| Framework | .NET 9 MAUI |
| Architecture | MVVM strict |
| Stockage | SQLite local (offline-first) |
| Cible prioritaire | Android |
| Compatibilité | iOS (prévu) |
| Modèle économique | Freemium (évolution future) |

**Objectif** : permettre à l'utilisateur de reprendre le contrôle de ses finances de manière simple, visuelle et sécurisée.

---

## 2. Stack Technique

| Élément | Technologie |
|---------|------------|
| Framework UI | .NET 9 MAUI |
| Langage | C# |
| Base de données | SQLite via `sqlite-net-pcl` (connexion synchrone `SQLiteConnection`) |
| Hash mot de passe | BCrypt.Net (`BCrypt.Net.BCrypt`, workFactor: 12) |
| Navigation | Shell (`Shell.Current.GoToAsync`) |
| Toolkit | CommunityToolkit.Maui (`UseMauiCommunityToolkit()`) |
| DI | `Microsoft.Extensions.DependencyInjection` (via `MauiApp.CreateBuilder()`) |
| Polices | OpenSans, MaterialIcons, Lora, FunnelDisplay |
| Cible Debug Android | `android-x64` pour émulateur x86_64 |

---

## 3. Architecture MVVM — Règles Absolues

### 3.1 Couches et responsabilités

| Couche | Namespace | Responsabilité |
|--------|-----------|---------------|
| **Models** | `MoneyMate.Models` | Entités de données uniquement (attributs SQLite) |
| **Views** | `MoneyMate.Views.*` | UI en XAML + code-behind minimal |
| **ViewModels** | `MoneyMate.ViewModels.*` | Logique de présentation, binding, commandes |
| **Services** | `MoneyMate.Services.Interfaces` / `MoneyMate.Services.Implementations` | Logique métier + accès DB |
| **Data** | `MoneyMate.Data.Context` | Contexte SQLite (`MoneyMateDbContext`, `DatabaseService`) |
| **Components** | `MoneyMate.Components` | Composants XAML réutilisables (Header, Footer, etc.) |
| **Helpers** | `MoneyMate.Helpers` | Utilitaires purs sans dépendances (validation, formatage) |
| **Behaviors** | `MoneyMate.Behaviors` | Comportements XAML attachés |
| **Converters** | `MoneyMate.Converters` | Convertisseurs de valeurs pour binding |
| **Configuration** | `MoneyMate.Configuration` | Routes, constantes applicatives et configuration Shell |
| **Services.Models** | `MoneyMate.Services.Models` | DTOs de synthèse retournés par les services |
| **Services.Results** | `MoneyMate.Services.Results` | Résultats métier typés (`ServiceResult`) |

### 3.2 Interdictions

- ❌ Logique métier dans une View ou un code-behind
- ❌ Accès direct à `MoneyMateDbContext` depuis un ViewModel
- ❌ Code-behind lourd (seuls `SetViewModel()` + `InitializeComponent()` + `OnAppearing` sont acceptés)
- ❌ SQL non paramétré (toujours utiliser `?` pour les paramètres)
- ❌ Couplage fort entre couches
- ❌ Duplication de logique
- ❌ `Shell.Current.GoToAsync` directement dans un ViewModel : passer par `INavigationService`
- ❌ Navigation avec des chaînes dispersées : utiliser `AppRoutes` et `NavigationParameterKeys`
- ❌ Modifier une `ObservableCollection` depuis un thread non UI
- ❌ Déplacer un bouton cliquable avec une marge négative hors bounds du layout : Android peut l'afficher mais ignorer le tap

### 3.3 Obligations

- ✅ Tout binding en XAML
- ✅ `ICommand` pour toutes les actions utilisateur
- ✅ `ObservableCollection<T>` pour les listes dynamiques UI
- ✅ `INotifyPropertyChanged` via `BaseViewModel`
- ✅ Services injectés via constructeur (DI)
- ✅ `async/await` pour toutes les opérations longues
- ✅ XML doc (`/// <summary>`) sur toutes les classes et méthodes publiques
- ✅ Constructeurs légers : aucun chargement SQLite, graphique ou liste lourde dans un constructeur
- ✅ Chargements UI via `InitializeAsync()` / `LoadAsync()` après le premier affichage
- ✅ Navigation via `INavigationService`
- ✅ Routes centralisées dans `AppRoutes`
- ✅ Paramètres Shell centralisés dans `NavigationParameterKeys`
- ✅ Résultats métier via `ServiceResult` / `ServiceResult<T>`
- ✅ Rafraîchissement inter-écrans via `IAppEventBus` quand une donnée métier change

---

## 4. Structure du Projet

```
MoneyMate/
├── App.xaml / App.xaml.cs
├── AppShell.xaml / AppShell.xaml.cs        ← Navigation Shell (routes)
├── MauiProgram.cs                          ← DI, fonts, plugins
├── MainPage.xaml / MainPage.xaml.cs        ← Page d'accueil publique
│
├── Models/                                 ← Entités SQLite
│   ├── User.cs
│   ├── Expense.cs
│   ├── Category.cs
│   ├── Budget.cs
│   ├── FixedCharge.cs
│   └── AlertThreshold.cs
│
├── Views/
│   ├── BasePage.cs                         ← Page de base (Header/Content/Footer)
│   ├── Auth/
│   │   ├── LoginPage.xaml(.cs)
│   │   └── RegisterPage.xaml(.cs)
│   ├── Dashboard/
│   │   └── DashboardPage.xaml(.cs)
│   ├── Expenses/
│   │   ├── ExpensesListPage.xaml(.cs)
│   │   ├── AddExpensePage.xaml(.cs)
│   │   ├── EditExpensePage.xaml(.cs)
│   │   ├── ExpenseDetailsPage.xaml(.cs)
│   │   └── QuickAddExpensePage.xaml(.cs)
│   ├── Categories/
│   │   ├── CategoriesListPage.xaml(.cs)
│   │   ├── AddCategoryPage.xaml(.cs)
│   │   └── EditCategoryPage.xaml(.cs)
│   ├── Budgets/
│   │   ├── BudgetsOverviewPage.xaml(.cs)
│   │   ├── AddBudgetPage.xaml(.cs)
│   │   └── EditBudgetPage.xaml(.cs)
│   ├── Alerts/
│   │   └── AlertThresholdPage.xaml(.cs)
│   ├── Calendar/
│   │   └── CalendarPage.xaml(.cs)
│   ├── Profile/
│   │   ├── ProfilePage.xaml(.cs)
│   │   ├── ChangePasswordPage.xaml(.cs)
│   │   └── DeleteAccountPage.xaml(.cs)
│   └── Errors/
│       ├── ErrorPage.xaml(.cs)
│       ├── NotFoundPage.xaml(.cs)
│       └── NoConnectionPage.xaml(.cs)
│
├── ViewModels/
│   ├── BaseViewModel.cs                    ← Classe abstraite (INotifyPropertyChanged)
│   ├── AuthenticatedViewModelBase.cs        ← Base des écrans connectés
│   ├── Forms/
│   │   └── FormViewModelBase.cs             ← Base création / édition
│   ├── Auth/
│   │   ├── LoginViewModel.cs
│   │   └── RegisterViewModel.cs
│   ├── Dashboard/
│   │   └── DashboardViewModel.cs
│   ├── Profile/
│   │   ├── ProfileViewModel.cs
│   │   ├── ChangePasswordViewModel.cs
│   │   └── DeleteAccountViewModel.cs
│   ├── Expenses/
│   ├── Budgets/
│   ├── Categories/
│   ├── Alerts/
│   ├── FixedCharges/
│   └── Calendar/
│
├── Services/
│   ├── Interfaces/
│   │   ├── IAuthenticationService.cs
│   │   ├── INavigationService.cs
│   │   ├── IAppEventBus.cs
│   │   └── I*Service.cs
│   └── Implementations/
│       ├── AuthenticationService.cs
│       ├── NavigationService.cs
│       ├── ShellRouteRegistry.cs
│       └── *Service.cs
│
├── Data/
│   └── Context/
│       ├── MoneyMateDbContext.cs            ← CRUD SQLite + seed données
│       └── DatabaseService.cs              ← Singleton thread-safe
│
├── Components/
│   ├── AuthenticatedHeader.xaml(.cs)       ← Header connecté (username + logout)
│   ├── AuthenticatedFooter.xaml(.cs)       ← Barre de navigation connecté
│   ├── EmptyStateView.xaml(.cs)            ← État vide réutilisable
│   ├── DonutChartView.cs                   ← Graphique donut GraphicsView
│   └── BudgetProgressBar.xaml(.cs)
│
├── Helpers/
│   ├── CurrencyHelper.cs                   ← Formatage monétaire (EUR, USD, etc.)
│   └── ValidationHelper.cs                 ← Validation email, mot de passe, montant, date
│
├── Behaviors/
│   ├── PasswordStrengthBehavior.cs         ← Indicateur force MDP temps réel
│   └── PasswordConfirmBehavior.cs          ← Vérification confirmation MDP
│
├── Converters/
│
└── Resources/
```

---

## 5. Modèles de Données (SQLite)

### 5.1 Tables existantes

| Table | Classe | Champs clés |
|-------|--------|-------------|
| `Users` | `User` | Id, Email (unique), PasswordHash (BCrypt), Devise, BudgetStartDay, Role, IsActive, CreatedAt |
| `Expenses` | `Expense` | Id, UserId, DateOperation, Amount, CategoryId, Note, IsFixedCharge |
| `Categories` | `Category` | Id, Name, Description, Color (#RRGGBB), Icon, DisplayOrder, IsActive, CreatedAt |
| `Budgets` | `Budget` | Id, UserId, CategoryId, Amount, PeriodType, StartDate, EndDate, IsActive, CreatedAt |
| `FixedCharges` | `FixedCharge` | Id, UserId, Name, Description, Amount, CategoryId, Frequency, DayOfMonth, IsActive |
| `AlertThresholds` | `AlertThreshold` | Id, UserId, BudgetId?, CategoryId?, ThresholdPercentage, AlertType, Message, IsActive, SendNotification, CreatedAt |

### 5.2 Conventions Models

- Attributs SQLite : `[Table]`, `[PrimaryKey, AutoIncrement]`, `[NotNull]`, `[Unique]`, `[Indexed]`, `[MaxLength]`, `[Ignore]`
- Navigation properties marquées `[Ignore]`
- Valeurs par défaut en C# (`= string.Empty`, `= DateTime.UtcNow`, etc.)
- `CreatedAt` toujours en `DateTime.UtcNow`
- Pas de logique métier dans les Models (sauf méthodes de calcul pures sur l'entité)

### 5.3 Catégories par défaut (seed)

Alimentation, Transport, Logement, Santé, Loisirs, Vêtements, Éducation, Autres.

---

## 6. Patterns de Code Existants

### 6.1 BaseViewModel

Classe abstraite dont héritent **tous** les ViewModels :

```csharp
public abstract class BaseViewModel : INotifyPropertyChanged
{
    // Propriétés : IsBusy, IsNotBusy, Title
    // Méthodes : SetProperty<T>(), OnPropertyChanged()
}
```

**Usage obligatoire** :
- Hériter de `BaseViewModel`
- Utiliser `SetProperty(ref _field, value)` pour toute propriété bindée
- Utiliser `IsBusy` pour verrouiller les commandes pendant le traitement

### 6.1.1 AuthenticatedViewModelBase

Les écrans connectés héritent de `AuthenticatedViewModelBase`.

Responsabilités :
- exposer `CurrentUser`, `CurrentUserId`, `CurrentDevise` ;
- centraliser `ErrorMessage` / `HasError` ;
- bloquer les actions si aucune session utilisateur active ;
- exécuter les traitements via `ExecuteBusyActionAsync()`.

Tout ViewModel connecté doit gérer proprement le cas `CurrentUserId <= 0` avec le message :

```text
Aucune session utilisateur active.
```

### 6.1.2 FormViewModelBase

Les écrans de création / édition héritent de `FormViewModelBase`.

Responsabilités :
- initialisation création / édition via `InitializeAsync(parameters)` ;
- validation via `ValidateForm()` ;
- commandes standard `SaveCommand`, `CancelCommand`, `DeleteCommand` ;
- état `CanSave`, `CanDelete`, `ValidationMessage` ;
- lecture des paramètres de navigation avec `NavigationParameterKeys`.

Quand un formulaire peut revenir vers plusieurs écrans, utiliser un paramètre contrôlé comme `NavigationParameterKeys.ReturnRoute` et whitelister les routes autorisées.

### 6.2 BasePage

Toutes les pages héritent de `BasePage` ou `BasePage<TViewModel>` :

```csharp
// Code-behind type :
public partial class XxxPage : BasePage
{
    private XxxViewModel ViewModel => (XxxViewModel)BindingContext;

    public XxxPage(XxxViewModel viewModel)
    {
        SetViewModel(viewModel);
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ViewModel.LoadData();
    }
}
```

`BasePage` impose automatiquement le squelette **Header / Content / Footer** via un `Grid` à 3 lignes.
- `ShowHeader` et `ShowFooter` : `BindableProperty` pour afficher/masquer
- `PageContent` : `BindableProperty` pour injecter le contenu central depuis XAML

### 6.3 Services

Architecture interface/implémentation :

```csharp
// Interface dans Services/Interfaces/
public interface IXxxService
{
    Task<Result> DoSomethingAsync(params);
}

// Implémentation dans Services/Implementations/
public class XxxService : IXxxService
{
    private readonly IMoneyMateDbContext _dbContext;

    public XxxService(IMoneyMateDbContext dbContext)
    {
        _dbContext = dbContext;
    }
}
```

Les constructeurs sans paramètre ne sont tolérés que pour préserver une compatibilité existante ou pour des tests ciblés. Le flux applicatif normal doit utiliser la DI.

Les services retournent des résultats explicites :

```csharp
Task<ServiceResult<T>> DoSomethingAsync(...);
Task<ServiceResult> DeleteSomethingAsync(...);
```

Règles :
- valider les entrées dans le service ;
- retourner un `ErrorCode` stable et un message utilisateur clair ;
- ne pas laisser remonter une exception technique vers le ViewModel ;
- centraliser les messages métier sensibles au doublon, session, droits ou validation.

### 6.4 Injection de dépendances (MauiProgram.cs)

Tout nouveau composant doit être enregistré dans `MauiProgram.cs` :

```csharp
// Services → Singleton
builder.Services.AddSingleton<IXxxService, XxxService>();

// ViewModels → Transient
builder.Services.AddTransient<XxxViewModel>();

// Pages → Transient
builder.Services.AddTransient<XxxPage>();
```

### 6.5 Navigation Shell

- Les routes racines publiques restent dans `AppShell.xaml`
- Les routes applicatives sont enregistrées dans `ShellRouteRegistry`
- Les noms de routes sont centralisés dans `AppRoutes`
- Les paramètres de routes sont centralisés dans `NavigationParameterKeys`
- Navigation depuis un ViewModel : `await NavigationService.NavigateToAsync(AppRoutes.Xxx)`
- Pattern `//` pour navigation absolue (reset de la pile)
- Les pages doivent être résolues à la demande ; pas de préchargement massif dans `AppShell`
- `NavigationService` normalise les routes et vérifie les droits via `IAuthenticationService.CanAccessRoute()`

### 6.6 Commandes de navigation

Chaque ViewModel expose ses propres commandes de navigation :

```csharp
public ICommand GoHomeCommand { get; }
public ICommand GoExpensesCommand { get; }
public ICommand GoBudgetCommand { get; }
public ICommand GoProfileCommand { get; }

// Dans le constructeur :
GoHomeCommand = new Command(async () => await NavigationService.NavigateToAsync(AppRoutes.Dashboard));
```

Pour transmettre des paramètres :

```csharp
await NavigationService.NavigateToAsync(
    AppRoutes.AddBudget,
    new Dictionary<string, object>
    {
        [NavigationParameterKeys.ReturnRoute] = AppRoutes.Dashboard
    });
```

Ne pas mettre de logique de décision métier dans le code-behind de la page.

### 6.7 Components réutilisables

| Composant | Usage | Propriétés bindables |
|-----------|-------|---------------------|
| `AuthenticatedHeader` | Pages connectées | `UserName`, `LogoutCommand` |
| `AuthenticatedFooter` | Pages connectées | `GoHomeCommand`, `GoExpensesCommand`, `GoBudgetCommand`, `GoProfileCommand` |
| `EmptyStateView` | États vides réutilisables | `Icon`, `Message`, `ActionText`, `ActionCommand` |
| `DonutChartView` | Répartition dépenses par catégorie | `Segments` |
| `BudgetProgressBar` | Progression consommation budget | Selon composant |

Règles pour les composants graphiques :
- un `GraphicsView` doit appeler `Invalidate()` quand ses données changent ;
- si la source est une `ObservableCollection`, écouter `INotifyCollectionChanged` ;
- ne pas supposer que le binding remplace toute la collection : les ViewModels peuvent faire `Clear()` puis `Add()`.

### 6.8 Helpers (utilitaires purs)

- `CurrencyHelper` : `GetSymbol(devise)`, `Format(amount, devise)` — pas de dépendance externe
- `ValidationHelper` : `IsValidEmail()`, `GetPasswordStrength()`, `GetPasswordStrengthLabel()`, `GetPasswordStrengthColor()`, `IsValidAmount()`, `IsValidDate()` — classe `partial` avec `[GeneratedRegex]`

### 6.9 Behaviors

- `PasswordStrengthBehavior` : évalue la force du MDP en temps réel, met à jour les barres visuelles et les critères
- `PasswordConfirmBehavior` : vérifie que la confirmation MDP correspond

---

## 7. Conventions de Code

### 7.1 Nommage

| Élément | Convention | Exemple |
|---------|-----------|---------|
| Classe | PascalCase | `ExpenseViewModel` |
| Interface | I + PascalCase | `IExpenseService` |
| Méthode publique | PascalCase + Async | `LoginAsync()` |
| Propriété publique | PascalCase | `UserName` |
| Champ privé | `_camelCase` | `_authService` |
| Variable locale | camelCase | `passwordHash` |
| Constante | UPPER_SNAKE_CASE | `MIN_PASSWORD_LENGTH` |
| Commande | Verbe + Command | `AddExpenseCommand` |
| ViewModel | Xxx + ViewModel | `ProfileViewModel` |
| Page | Xxx + Page | `ProfilePage` |
| Service | Xxx + Service | `AuthenticationService` |
| Behavior | Xxx + Behavior | `PasswordStrengthBehavior` |
| Helper | Xxx + Helper | `CurrencyHelper` |

### 7.2 Formatage

- Indentation : **4 espaces** (jamais de tabulations)
- Encodage : **UTF-8 avec BOM**
- Fins de ligne : **CRLF**
- Nouvelle ligne finale : **oui**
- Accolades : style Allman (nouvelle ligne)
- `var` autorisé quand le type est évident
- Expression-bodied members autorisés pour les one-liners

### 7.3 Documentation

```csharp
/// <summary>
/// Description de la classe ou méthode.
/// </summary>
/// <param name="paramName">Description du paramètre</param>
/// <returns>Description du retour</returns>
```

Obligatoire sur : classes, interfaces, méthodes publiques, propriétés publiques des Models.

### 7.4 Organisation d'un ViewModel

```csharp
public class XxxViewModel : BaseViewModel
{
    // 1. Champs privés readonly (services injectés)
    private readonly IXxxService _service;

    // 2. Champs privés (backing fields)
    private string _name = string.Empty;

    // 3. Propriétés publiques (avec SetProperty)
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    // 4. Commandes (ICommand)
    public ICommand SaveCommand { get; }

    // 5. Constructeur (injection + init commandes)
    public XxxViewModel(IXxxService service)
    {
        _service = service;
        Title = "Titre Page";
        SaveCommand = new Command(async () => await SaveAsync(), CanSave);
    }

    // 6. Méthodes privées (CanExecute, actions, chargement)
    private bool CanSave() => !IsBusy && !string.IsNullOrWhiteSpace(Name);

    private async Task SaveAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            // ...
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur : {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### 7.5 Pattern try/catch dans les ViewModels

```csharp
try
{
    IsBusy = true;
    // opération async
}
catch (Exception ex)
{
    System.Diagnostics.Debug.WriteLine($"Erreur NomMethode : {ex.Message}");
    // Message utilisateur si besoin
}
finally
{
    IsBusy = false;
    ((Command)XxxCommand).ChangeCanExecute();
}
```

---

## 8. Sécurité

### 8.1 Authentification

- Mot de passe **hashé avec BCrypt** (workFactor: 12)
- `BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12)`
- `BCrypt.Net.BCrypt.Verify(password, hash)`
- **Jamais** de mot de passe en clair en base ou en mémoire prolongée

### 8.2 Validation mot de passe

Exigences minimales (vérifiées par `AuthenticationService.ValidatePasswordStrength`) :
- 8 caractères minimum
- 1 majuscule
- 1 minuscule
- 1 chiffre
- 1 caractère spécial

### 8.3 SQLite

- Requêtes paramétrées **uniquement** : `Database.Execute("DELETE FROM X WHERE Id = ?", id)`
- LINQ via `sqlite-net-pcl` : `Database.Table<T>().Where(...)` (paramétré nativement)
- Jamais de concaténation de chaînes dans les requêtes SQL

### 8.4 RGPD

- Suppression compte = suppression **totale** en cascade dans une transaction :
  1. AlertThresholds → 2. Expenses → 3. FixedCharges → 4. Budgets → 5. Users
- Méthode : `MoneyMateDbContext.DeleteAllUserData(userId)`
- Isolation par `UserId` sur toutes les requêtes de données

### 8.5 Session

- Utilisateur courant stocké en mémoire (`_currentUser` dans `AuthenticationService`)
- "Se souvenir de moi" via `Preferences` (email uniquement, jamais le mot de passe)

---

## 9. Règles Métier

| Règle | Formule / Description |
|-------|----------------------|
| **RG1 – Budget consommé** | `(Somme dépenses catégorie / Budget catégorie) × 100` |
| **RG2 – Déclenchement seuil** | Si `Budget consommé ≥ ThresholdPercentage` → alerte utilisateur |
| **RG3 – Types d'alerte** | `Warning` (approche du seuil), `Critical` (dépassement) |
| **RG4 – Charges fixes** | Récurrentes (Monthly, Quarterly, Yearly), apparaissent dans le calendrier |
| **RG5 – Catégories par défaut** | 8 catégories seedées à l'initialisation de la DB |
| **RG6 – Suppression catégorie** | Uniquement si non utilisée par des dépenses/budgets |
| **RG7 – Devise** | EUR par défaut, configurable par utilisateur (EUR, USD, GBP, CHF, CAD) |
| **RG8 – Cycle budgétaire** | Jour de début configurable (`BudgetStartDay`, 1-31) |
| **RG9 – Montant** | Toujours positif (`decimal`), validé par `ValidationHelper.IsValidAmount()` |
| **RG10 – Date** | Ne peut pas être dans le futur, validé par `ValidationHelper.IsValidDate()` |
| **RG11 – Budget mensuel unique** | Un seul budget par utilisateur et par mois/année |
| **RG12 – Budget futur interdit** | Impossible de créer un budget pour un mois futur |
| **RG13 – Doublon budget** | Message utilisateur : `Un budget existe déjà pour ce mois.` |
| **RG14 – Dashboard sans budget** | Si aucun budget du mois n'existe, afficher une action `Créer mon budget` vers `AddBudgetPage` |
| **RG15 – Refresh Dashboard** | Après création d'un budget, publier `AppDataChangeKind.Budgets` puis revenir au Dashboard si demandé |

**Important** : toute logique métier doit être centralisée dans les **Services**, jamais dans les ViewModels.

---

## 10. Modules Fonctionnels

### MODULE 1 — Authentification & Compte

| Fonctionnalité | Routes | État |
|---------------|--------|------|
| Création de compte | `RegisterPage` | ✅ Implémenté |
| Connexion | `LoginPage` | ✅ Implémenté |
| Déconnexion | via `LogoutCommand` | ✅ Implémenté |
| Profil utilisateur | `ProfilePage` | ✅ Implémenté |
| Changement mot de passe | `ChangePasswordPage` | ✅ Implémenté |
| Suppression compte (RGPD) | `DeleteAccountPage` | ✅ Implémenté |

### MODULE 2 — Dépenses

| Fonctionnalité | Routes | État |
|---------------|--------|------|
| Liste chronologique | `ExpensesListPage` | ✅ Implémenté |
| Ajout dépense | `AddExpensePage` | 🔧 En cours |
| Modification | `EditExpensePage` | 🔧 En cours |
| Détails | `ExpenseDetailsPage` | 🔧 En cours |
| Ajout rapide | `QuickAddExpensePage` | 🔧 En cours |
| Filtrage (catégorie, période, charge fixe) | — | 📋 À faire |

La liste dépenses utilise un ViewModel d'affichage (`ExpenseListItemViewModel`) pour exposer à la CollectionView le libellé, la catégorie, la note, le montant formaté, la date et la commande d'ouverture. Ne pas binder directement une CollectionView complexe sur les entités SQLite.

Sur Android, la page `ExpensesListPage` doit conserver une `CollectionView` virtualisée dans une ligne `Grid` dédiée. Ne pas l'imbriquer dans un `ScrollView`, car cela force la mesure complète des items et peut provoquer un ANR (`MoneyMate isn't responding`) après ajout rapide ou retour sur la liste.

### MODULE 3 — Catégories

| Fonctionnalité | Routes | État |
|---------------|--------|------|
| Liste catégories | `CategoriesListPage` | 🔧 En cours |
| Ajout catégorie personnalisée | `AddCategoryPage` | 🔧 En cours |
| Modification catégorie | `EditCategoryPage` | 🔧 En cours |
| Suppression (si non utilisée) | — | 📋 À faire |

### MODULE 4 — Budgets

| Fonctionnalité | Routes | État |
|---------------|--------|------|
| Vue d'ensemble | `BudgetsOverviewPage` | ✅ Implémenté |
| Ajout budget | `AddBudgetPage` | ✅ Implémenté |
| Modification budget | `EditBudgetPage` | ✅ Implémenté |
| Budget mensuel unique | Service | ✅ Implémenté |
| Validation montant > 0 | ViewModel + Service | ✅ Implémenté |
| Détection doublon mois/année | Service | ✅ Implémenté |
| Retour Dashboard après création depuis Dashboard | `ReturnRoute` | ✅ Implémenté |
| Calcul automatique (consommé/restant) | Service | ✅ Implémenté |
| Déclenchement alertes | — | 🔧 En cours |

Flux Dashboard sans budget :

1. `DashboardService.GetDashboardSummaryAsync()` renseigne `HasCurrentMonthBudget`.
2. `DashboardViewModel` expose `IsCurrentMonthBudgetMissing`.
3. `DashboardPage` affiche un bouton icône `+` dans la carte du donut.
4. `CreateBudgetCommand` navigue vers `AddBudgetPage` avec `ReturnRoute = AppRoutes.Dashboard`.
5. `BudgetFormViewModel` crée le budget, publie `AppDataChangeKind.Budgets`, puis revient au Dashboard.
6. `DashboardViewModel.RefreshIfNeededAsync()` recharge les données automatiquement.

### MODULE 5 — Visualisation & Statistiques

| Fonctionnalité | État |
|---------------|------|
| Donut dépenses par catégorie du mois | ✅ Implémenté |
| Graphique barres (comparaison mois) | 📋 À faire |
| Graphique courbes (évolution dans le temps) | 📋 À faire |
| Indicateur progression épargne | 📋 À faire |

### MODULE 6 — Calendrier Charges Fixes

| Fonctionnalité | Routes | État |
|---------------|--------|------|
| Vue calendrier mensuel | `CalendarPage` | 🔧 En cours |
| Mise en évidence charges fixes | — | 📋 À faire |
| Notifications préventives | — | 📋 À faire |

### MODULE 7 — Alertes

| Fonctionnalité | Routes | État |
|---------------|--------|------|
| Configuration seuils | `AlertThresholdPage` | 🔧 En cours |
| Notifications | — | 📋 À faire |

### MODULE 8 — Export / Import (Évolutif)

| Fonctionnalité | État |
|---------------|------|
| Export PDF bilan mensuel | 📋 À faire |
| Export CSV dépenses | 📋 À faire |
| Import CSV | 🔮 Évolution future (Premium) |
| Import PDF | 🔮 Évolution future (Premium) |

### MODULE 9 — Pages d'erreur

| Page | Route | Usage |
|------|-------|-------|
| Erreur générique | `ErrorPage` | Erreur inattendue |
| Page introuvable | `NotFoundPage` | Route invalide |
| Pas de connexion | `NoConnectionPage` | Réseau indisponible |

---

## 11. Gestion des Erreurs

- Pas de crash silencieux : toujours un `try/catch` avec `Debug.WriteLine`
- Message utilisateur clair en cas d'échec (via propriété bindée, pas `DisplayAlert` dans le ViewModel)
- Pages d'erreur dédiées (`ErrorPage`, `NotFoundPage`, `NoConnectionPage`)
- Vérification `IsBusy` en début de toute méthode async pour éviter les doubles appels

---

## 12. Créer un Nouveau Module — Checklist

Quand on ajoute une fonctionnalité complète (ex: un nouveau CRUD) :

1. **Model** : créer la classe dans `Models/`, avec attributs SQLite
2. **DbContext** : ajouter la table dans `MoneyMateDbContext.InitializeDatabase()` + méthodes CRUD dans une `#region`
3. **Interface Service** : créer dans `Services/Interfaces/`
4. **Implémentation Service** : créer dans `Services/Implementations/`
5. **ViewModel** : créer dans `ViewModels/NomModule/`, hériter de `BaseViewModel`
6. **Page XAML** : créer dans `Views/NomModule/`, hériter de `BasePage`
7. **Code-behind** : pattern `SetViewModel()` + `InitializeComponent()` + `OnAppearing()`
8. **DI** : enregistrer Service (Singleton), ViewModel (Transient), Page (Transient) dans `MauiProgram.cs`
9. **Shell** : ajouter la route dans `AppShell.xaml`
10. **Tests** : écrire les tests du Service et du ViewModel

---

## 13. UI / UX

### 13.1 Charte couleurs

| Usage | Couleur |
|-------|---------|
| Primaire | `#6B7A8F` |
| Alerte / Warning | `#F6B092` |
| Danger / Critical | `#D9534F` |
| Succès | `#6CC57C` |
| Force MDP faible | `#E57373` |
| Force MDP moyen | `#FFB74D` → `#FFF176` |
| Force MDP fort | `#6CC57C` |
| Fond neutre | `#EEEEEE` |

### 13.2 Règles UI

- Mobile-first (Android prioritaire)
- Lisibilité maximale
- Feedback visuel immédiat (indicateurs, barres de force, couleurs)
- Pas de page blanche : toujours un état vide explicite
- Accessibilité : tailles de police lisibles, contrastes suffisants
- Les boutons icônes doivent rester dans les bounds réels du layout pour conserver leur zone tactile Android
- Les actions principales doivent avoir une `SemanticProperties.Description`
- Sur le Dashboard, le bouton de création de budget du mois est une icône `+` visible uniquement quand `IsCurrentMonthBudgetMissing == true`
- Ne pas afficher du texte décoratif qui encombre le donut : privilégier les montants et les actions utiles

---

## 14. Tests

### 14.1 Obligatoire

- Tests unitaires des Services (logique métier)
- Tests unitaires des ViewModels (commandes, états)
- Tests des calculs métier (budget consommé, seuils)
- Tests des Helpers (validation email, force MDP, formatage devise)

### 14.2 Convention

```csharp
[TestMethod]
public async Task LoginAsync_WithValidCredentials_ReturnsUser()
{
    // Arrange
    // Act
    // Assert
    Assert.IsNotNull(result);
}
```

Nommage : `MethodName_Scenario_ExpectedResult`

### 14.3 Commandes utiles

Tests unitaires complets :

```powershell
dotnet test tests\UnitTests\UnitTests.csproj
```

Tests ciblés Dashboard/Budget :

```powershell
dotnet test tests\UnitTests\UnitTests.csproj --filter "FullyQualifiedName~DashboardViewModelTests|FullyQualifiedName~BudgetFormViewModelTests|FullyQualifiedName~DashboardServiceTests"
```

Build Android Debug :

```powershell
dotnet build MoneyMate\MoneyMate.csproj -f net9.0-android -c Debug
```

Si un test complet échoue hors périmètre, documenter le test en échec dans le compte rendu et exécuter les tests ciblés liés à la modification.

---

## 15. Workflow Git

### 15.1 Branches

```
main            ← production stable
develop         ← intégration
feat/*          ← nouvelles fonctionnalités
fix/*           ← corrections de bugs
refactor/*      ← refactorings
```

### 15.2 Processus

```bash
git checkout develop
git pull
git checkout -b feat/nom-fonctionnalite
# ... développement ...
git add .
git commit -m "feat: description"
git push origin feat/nom-fonctionnalite
# → Créer une Pull Request vers develop
```

### 15.3 Convention de commit

```
feat: ajout gestion budgets
fix: correction bug login
refactor: nettoyage services
style: formatage code
docs: mise à jour contributing
test: ajout tests budget service
```

### 15.4 Pull Request

Doit contenir :
- Description claire du changement
- Screenshots UI si modification visuelle
- Impact technique
- Tests réalisés
- Respect de ce CONTRIBUTING.md vérifié

### 15.5 Travail avec une IA / Codex

- Toujours créer une branche avant modification significative.
- Préférer des changements petits et vérifiables.
- Ne pas écraser les changements locaux non liés.
- L'IA doit expliquer les fichiers modifiés, les tests lancés et les limites connues.
- Après modification UI, tester sur Android ou au minimum compiler le projet MAUI concerné.
- Les messages de commit doivent être courts, en français, avec préfixe conventional commit.

Exemples :

```text
feat: ajoute le bouton budget au dashboard
fix: corrige le rafraichissement du donut
docs: met a jour les regles de contribution
```

---

## 16. RBAC — Évolution Future (Freemium)

| Rôle | Droits |
|------|--------|
| **Utilisateur Standard** | CRUD dépenses, budgets, catégories, visualisation, export simple |
| **Utilisateur Premium** | Import données, export avancé, alertes personnalisées, multi-profils |
| **Administrateur** | Gestion globale comptes, stats d'usage, configuration système |

Le champ `User.Role` existe déjà (`"User"` par défaut, `"Admin"` possible).

---

## 17. Roadmap Évolutions Futures

- 🔔 Notifications push (alertes budget)
- 📄 Export PDF du bilan mensuel
- 📊 Statistiques avancées (graphiques)
- 💎 Modèle freemium (RBAC)
- ☁️ Synchronisation cloud
- 📱 Multi-device
- 📥 Import intelligent (CSV/PDF)
- 🌐 Multi-langue

---

## 18. Exigences Non Fonctionnelles

| Exigence | Critère |
|----------|---------|
| Performance | Premier écran utile < 2 secondes sur mobile cible |
| Réactivité UI | Mise à jour graphique immédiate |
| Sécurité | MDP hashé, aucune donnée sensible en clair, RGPD |
| Maintenabilité | MVVM strict, code documenté, testable |
| Navigation | Shell cohérente |
| Compatibilité | Android (prioritaire), iOS (extensible) |

### 18.1 Règles startup mobile-first

Le démarrage mobile doit rester minimal :

- charger uniquement ce qui est nécessaire pour afficher le premier écran ;
- ne jamais bloquer le thread UI avec SQLite, seed, graphiques ou calculs de dashboard ;
- lancer l'initialisation locale en arrière-plan quand elle n'est pas indispensable à la navigation ;
- restaurer la session avant de charger le dashboard complet ;
- afficher un dashboard minimal puis remplir les blocs progressivement ;
- préférer des routes Shell et pages `Transient` créées à la demande ;
- profiler en Release avant les gros refactors de performance.

Configuration Android Debug actuelle :

```xml
<PropertyGroup Condition="'$(TargetFramework)' == 'net9.0-android' and '$(Configuration)' == 'Debug'">
    <RuntimeIdentifier>android-x64</RuntimeIdentifier>
    <EmbedAssembliesIntoApk>false</EmbedAssembliesIntoApk>
    <UseAppHost>false</UseAppHost>
</PropertyGroup>
```

Cette configuration cible l'AVD x86_64. Pour tester un téléphone physique ARM, utiliser une configuration dédiée afin de ne pas casser l'expérience émulateur.

### 18.2 Bindings XAML

Les compiled bindings sont prioritaires sur les pages mobiles critiques. Chaque `DataTemplate` doit déclarer son propre `x:DataType`.

Warnings à corriger avant stabilisation :

- `XC0022` : binding non compilé ;
- `XC0024` : `x:DataType` hérité d'un mauvais scope ;
- `XC0045` : propriété bindée absente ou mauvais type de ViewModel.

Ne pas masquer ces warnings avec `x:DataType="x:Object"` : corriger le type ou le binding.

### 18.3 Listes MAUI Android

Les listes doivent rester légères et virtualisées :

- ne jamais placer une `CollectionView` dans un `ScrollView` sur une page mobile critique ;
- utiliser un parent `Grid` avec une ligne `*` pour donner une hauteur bornée à la liste ;
- préférer `ItemSizingStrategy="MeasureFirstItem"` quand les cartes ont une hauteur stable ;
- garder les templates d'items simples : pas de logique métier, pas de requête service, pas de conversion coûteuse ;
- mapper les données côté ViewModel vers un DTO d'affichage (`ExpenseListItemViewModel`, `ExpenseDisplayDto`, etc.) avant de remplir la collection ;
- ne mettre à jour l'`ObservableCollection` que depuis le thread UI ;
- charger les données avec `LoadAsync()` / `RefreshAsync()` et éviter tout chargement en constructeur.

Exemple attendu :

```xml
<Grid RowDefinitions="Auto,*">
    <CollectionView Grid.Row="1"
                    ItemsSource="{Binding Items}"
                    ItemSizingStrategy="MeasureFirstItem">
        <CollectionView.ItemTemplate>
            <DataTemplate x:DataType="viewModels:ItemViewModel">
                <!-- carte item -->
            </DataTemplate>
        </CollectionView.ItemTemplate>
    </CollectionView>
</Grid>
```

### 18.4 Polices MAUI

Tout `FontFamily` utilisé en XAML doit avoir un alias déclaré dans `MauiProgram.cs`.

Aliases actuellement attendus :

```csharp
fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
fonts.AddFont("OpenSans-Regular.ttf", "OpenSans");
fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemiBold");
fonts.AddFont("Lora-VariableFont_wght.ttf", "Lora");
fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
```

Règles :

- ne pas utiliser un nom de fichier comme `FontFamily` si l'alias n'est pas déclaré ;
- vérifier les logs Android après modification UI : `Font asset not found OpenSans` ou `Font asset not found Lora` indique un alias manquant ou un asset non déployé ;
- après correction de police, faire un rebuild/redeploy complet ; si l'émulateur garde de vieux assets FastDev, désinstaller l'application puis redéployer ;
- limiter le nombre de familles sur les listes longues : chaque police supplémentaire augmente le coût de mesure/rendu Android.

Références Microsoft Learn :

- [Improve app performance - .NET MAUI](https://learn.microsoft.com/dotnet/maui/deployment/performance)
- [Compiled bindings - .NET MAUI](https://learn.microsoft.com/dotnet/maui/fundamentals/data-binding/compiled-bindings)
- [Android emulator hardware acceleration](https://learn.microsoft.com/dotnet/maui/android/emulator/hardware-acceleration)

---

### 18.5 GraphicsView et refresh visuel

Les composants basés sur `GraphicsView` sont sensibles au cycle de rendu Android.

Règles :

- appeler `Invalidate()` quand les données affichées changent ;
- si la donnée bindée est une collection mutable, écouter `INotifyCollectionChanged` ;
- déclencher l'invalidation sur le dispatcher UI si nécessaire ;
- garder des dimensions stables (`WidthRequest`, `HeightRequest`, `HeightRequest` du conteneur si superposition) ;
- éviter de superposer des éléments interactifs avec des marges négatives ;
- préférer un parent `Grid` borné quand un bouton est posé autour du graphique.

Cas de référence : `DonutChartView` écoute `Segments` et se redessine quand `TopCategorySegments` est vidé/rempli par le `DashboardViewModel`.

---

## 19. Récapitulatif des Routes Shell

| Route | Page | Module |
|-------|------|--------|
| `MainPage` | Accueil | Public |
| `LoginPage` | Connexion | Auth |
| `RegisterPage` | Inscription | Auth |
| `DashboardPage` | Tableau de bord | Dashboard |
| `ProfilePage` | Mon profil | Profil |
| `ChangePasswordPage` | Changer MDP | Profil |
| `DeleteAccountPage` | Supprimer compte | Profil |
| `ExpensesListPage` | Liste dépenses | Dépenses |
| `AddExpensePage` | Ajouter dépense | Dépenses |
| `EditExpensePage` | Modifier dépense | Dépenses |
| `ExpenseDetailsPage` | Détails dépense | Dépenses |
| `QuickAddExpensePage` | Ajout rapide | Dépenses |
| `CategoriesListPage` | Liste catégories | Catégories |
| `AddCategoryPage` | Ajouter catégorie | Catégories |
| `EditCategoryPage` | Modifier catégorie | Catégories |
| `BudgetsOverviewPage` | Vue budgets | Budgets |
| `AddBudgetPage` | Ajouter budget | Budgets |
| `EditBudgetPage` | Modifier budget | Budgets |
| `AlertThresholdPage` | Seuils d'alerte | Alertes |
| `CalendarPage` | Calendrier | Calendrier |
| `ErrorPage` | Erreur | Erreurs |
| `NotFoundPage` | Page introuvable | Erreurs |
| `NoConnectionPage` | Pas de connexion | Erreurs |

---

> **Ce fichier est la seule source de vérité.** Toute contribution, humaine ou IA, doit s'y conformer strictement.
