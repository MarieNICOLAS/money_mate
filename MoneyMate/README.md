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
dotnet maui run -f net9.0-android
```

------------------------------------------------------------------------

## 🗄️ Base de données

L'application utilise **SQLite en local**.

-   La base est créée automatiquement au premier lancement
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
