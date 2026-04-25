# 💰 Money Mate

Application mobile de suivi des dépenses personnelles développée en
**.NET 9 MAUI** avec architecture **MVVM** et stockage local **SQLite**.

------------------------------------------------------------------------

## 🚀 Stack Technique

-   .NET 9
-   .NET MAUI
-   Architecture MVVM
-   SQLite (stockage local)
-   XAML (UI)
-   C#

------------------------------------------------------------------------

## 📦 Prérequis

Avant d'installer le projet, assurez-vous d'avoir :

-   Visual Studio 2022 (17.9+ recommandé)
-   Workload **.NET MAUI**
-   .NET SDK 9 installé\
-   Android SDK configuré (via Visual Studio)
-   Émulateur Android ou appareil physique

Vérification :

``` bash
dotnet --version
```

Doit retourner une version 9.x

------------------------------------------------------------------------

## ⚙️ Installation du projet

### 1️⃣ Cloner le repository

``` bash
git clone https://github.com/ton-compte/money-mate.git
cd money-mate
```

### 2️⃣ Restaurer les dépendances

``` bash
dotnet restore
```

### 3️⃣ Vérifier les workloads MAUI

``` bash
dotnet workload list
```

Si MAUI n'est pas installé :

``` bash
dotnet workload install maui
```

------------------------------------------------------------------------

## ▶️ Lancer l'application

### Via Visual Studio

1.  Ouvrir `MoneyMate.sln`
2.  Sélectionner :
    -   Android Emulator
    -   ou appareil physique
3.  Appuyer sur **Run**

### Via CLI

``` bash
dotnet build
dotnet build MoneyMate/MoneyMate.csproj -f net9.0-android
```

------------------------------------------------------------------------

## 📱 Android et démarrage mobile-first

Android est la cible prioritaire. Le profil `Debug` Android est optimisé
pour l'émulateur Pixel API 35 x86_64 :

-   `RuntimeIdentifier=android-x64`
-   `EmbedAssembliesIntoApk=false`
-   `UseAppHost=false`

Ces réglages réduisent le coût du build/déploiement Debug et évitent le
calcul multi-ABI inutile pendant l'émulation. Pour un appareil physique ARM,
il faudra adapter le Runtime Identifier ou utiliser une configuration dédiée.

Principes de démarrage :

-   afficher le premier écran le plus vite possible ;
-   garder les constructeurs de pages et ViewModels légers ;
-   charger les données via `InitializeAsync()` / `LoadAsync()` après affichage ;
-   éviter de charger dépenses, budgets, graphiques et charges fixes avant le
    premier rendu ;
-   initialiser SQLite et la seed en arrière-plan quand la session n'en dépend pas.

Diagnostics Android utiles :

``` bash
dotnet build MoneyMate/MoneyMate.csproj -f net9.0-android --no-restore
"C:\Program Files (x86)\Android\android-sdk\emulator\emulator.exe" -list-avds
"C:\Program Files (x86)\Android\android-sdk\emulator\emulator.exe" -accel-check
"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" devices -l
```

Si Visual Studio reste bloqué sur `emulator.exe -avd ...`, le problème est
probablement côté AVD avant l'exécution de l'application. Vérifier :

-   image système x86_64 ;
-   WHPX/Hyper-V actif ;
-   Cold Boot de l'AVD ;
-   Wipe Data si le snapshot ou `userdata-qemu.img` est corrompu ;
-   test sur appareil Android physique si l'AVD reste instable.

Références Microsoft Learn consultées :

-   [Performance .NET MAUI](https://learn.microsoft.com/dotnet/maui/deployment/performance)
-   [Compiled bindings .NET MAUI](https://learn.microsoft.com/dotnet/maui/fundamentals/data-binding/compiled-bindings)
-   [Accélération matérielle Android Emulator](https://learn.microsoft.com/dotnet/maui/android/emulator/hardware-acceleration)

------------------------------------------------------------------------

## 🗄️ Base de données

L'application utilise **SQLite en local**.

-   La base est créée automatiquement au premier lancement
-   Les données de démonstration incluent `demo@moneymate.fr`
-   L'initialisation est déclenchée au démarrage sans bloquer le premier écran
-   Les tables sont générées via les modèles
-   Le fichier `.db` est stocké dans le dossier local de l'application

### Schéma principal

Tables principales :

-   User
-   Expense
-   Category
-   Budget
-   FixedCharge
-   AlertThreshold

Aucune configuration serveur n'est nécessaire.

------------------------------------------------------------------------

## 🧱 Architecture

Projet structuré selon le pattern MVVM :

    MoneyMate/
    │
    ├── Models/
    ├── Views/
    ├── ViewModels/
    ├── Services/
    ├── Data/
    ├── Helpers/
    ├── Converters/
    └── Resources/

Principe :

-   Views → XAML uniquement (Binding)
-   ViewModels → logique de présentation
-   Services → logique métier & accès BDD
-   Models → entités mappées SQLite
-   DTO/ViewModels d'affichage pour les listes complexes, afin de ne pas exposer
    directement les entités SQLite à l'UI

------------------------------------------------------------------------

## 🔐 Sécurité

-   Mots de passe hashés
-   Données stockées localement
-   Suppression complète du compte (RGPD compliant)
-   Aucune API externe
-   Fonctionnement offline-first

------------------------------------------------------------------------

## 🧪 Mode Développement

Pour activer le mode debug :

-   Build configuration : `Debug`
-   Activer le Hot Reload MAUI

Logs accessibles via :

-   Output Visual Studio
-   Logcat Android

### Vérifications récentes

-   Build Windows `net9.0-windows10.0.19041.0` : OK
-   Build Android `net9.0-android` : OK avec warnings existants
-   Tests unitaires mapping dépenses : OK (`7/7`)

Warnings XAML encore à traiter en priorité :

-   `AlertThresholdPage`
-   `AddExpensePage`
-   `EditExpensePage`
-   `ExpenseDetailsPage`
-   `FixedChargesPage`

------------------------------------------------------------------------

## 📂 Branching Strategy

    main      → version stable
    develop   → intégration
    feature/* → nouvelles fonctionnalités

------------------------------------------------------------------------

## 🛠️ Commandes utiles

Build :

``` bash
dotnet build
```

Clean :

``` bash
dotnet clean
```

Rebuild complet :

``` bash
dotnet clean
dotnet restore
dotnet build
```

------------------------------------------------------------------------

## 📱 Cible actuelle

-   Android (prioritaire)
-   Architecture compatible iOS (extension future)

------------------------------------------------------------------------

## 📌 Roadmap (technique)

-   Export PDF
-   Export CSV
-   Notifications locales
-   Optimisation performances graphiques
-   Mode Premium (RBAC anticipé)

------------------------------------------------------------------------

## 👤 Auteur

Marie Nicolas\
Master 1 -- Lead Dev FullStack\
Année 2025-2026
