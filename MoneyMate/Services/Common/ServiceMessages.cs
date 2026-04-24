namespace MoneyMate.Services.Common
{
    /// <summary>
    /// Centralise les messages génériques réutilisables par les services.
    /// Évite la duplication et facilite l'harmonisation des retours utilisateur.
    /// </summary>
    public static class ServiceMessages
    {
        public const string UnexpectedError = "Une erreur inattendue est survenue.";
        public const string InvalidUser = "Utilisateur invalide.";
        public const string InvalidInput = "Les informations demandées sont invalides.";
        public const string NotFound = "Élément introuvable.";
        public const string CreateSuccess = "Création effectuée avec succès.";
        public const string UpdateSuccess = "Mise à jour effectuée avec succès.";
        public const string DeleteSuccess = "Suppression effectuée avec succès.";
        public const string LoadError = "Une erreur est survenue lors du chargement des données.";
        public const string CreateError = "Une erreur est survenue lors de la création.";
        public const string UpdateError = "Une erreur est survenue lors de la mise à jour.";
        public const string DeleteError = "Une erreur est survenue lors de la suppression.";
        public const string ValidationError = "Les données fournies sont invalides.";
        public const string AccessDenied = "Accès refusé.";
        public const string SessionRequired = "Vous devez être connecté pour accéder à cette fonctionnalité.";
    }
}
