# Rapport de tests — ECommerce.Catalog.Api

**Date :** 2026-06-26
**Branche :** `feat/tests-catalog`
**Framework :** xUnit 2.x · .NET 10 · `WebApplicationFactory<Program>`
**Résultat global : 10 / 10 ✅**

---

## Résumé

| Métrique | Valeur |
|---|---|
| Tests exécutés | 10 |
| Réussis | 10 |
| Échoués | 0 |
| Ignorés | 0 |
| Durée totale | ~2,9 s |

---

## Résultats détaillés

### Classe `GetProductsTests`

| # | Nom du test | Statut | Durée |
|---|---|---|---|
| 1 | `GetProducts_ReturnsOk_WithFourSeededProducts` | ✅ Réussi | ~1 s |
| 2 | `GetProductById_ExistingId_ReturnsOkWithCorrectProduct` | ✅ Réussi | ~117 ms |
| 3 | `GetProductById_NonExistentId_ReturnsNotFound` | ✅ Réussi | ~9 ms |

### Classe `CreateProductTests`

| # | Nom du test | Statut | Durée |
|---|---|---|---|
| 4 | `CreateProduct_ValidRequest_ReturnsCreatedWithLocationHeader` | ✅ Réussi | ~21 ms |
| 5 | `CreateProduct_NullDescription_ReturnsCreatedWithNullDescription` | ✅ Réussi | ~10 ms |
| 6 | `CreateProduct_ZeroStock_ReturnsCreatedWithZeroStock` | ✅ Réussi | ~4 ms |
| 7 | `CreateProduct_ThenGetById_ReturnsSameProduct` | ✅ Réussi | ~129 ms |
| 8 | `CreateProduct_VariousValidInputs` — Laptop / 1299,99 € / stock 15 | ✅ Réussi | ~1 s |
| 9 | `CreateProduct_VariousValidInputs` — USB Cable / null desc / stock 500 | ✅ Réussi | ~13 ms |
| 10 | `CreateProduct_VariousValidInputs` — Monitor Stand / prix 0 / stock 30 | ✅ Réussi | ~4 ms |

---

## Couverture fonctionnelle

### Endpoints testés

| Endpoint | Cas couverts |
|---|---|
| `GET /api/products` | 200 + liste des 4 produits seedés |
| `GET /api/products/{id}` | 200 (id existant), 404 (id inexistant) |
| `POST /api/products` | 201 + header `Location`, corps retourné correct |

### Cas limites couverts

- Description optionnelle (`null`) acceptée sans erreur
- Stock à zéro accepté
- Round-trip POST → GET : le produit créé est retrouvable par son ID
- `[Theory]` sur 3 shapes différentes (prix élevé, description nulle, prix zéro)

---

## Architecture de test

### `CatalogApiFactory`

```csharp
public class CatalogApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remplace la DB "catalog" par une DB in-memory isolée par instance de factory
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<CatalogDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<CatalogDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }
}
```

**Point clé :** `_dbName` est capturé en champ d'instance (fixe), et non dans la lambda.
`DbContextOptions<CatalogDbContext>` étant enregistré avec une durée de vie **scoped**,
une valeur `Guid.NewGuid()` dans la lambda aurait produit une base différente à chaque
requête HTTP — rendant impossible la persistance inter-requêtes dans les tests.

### Isolation

Chaque classe de test (`IClassFixture<CatalogApiFactory>`) reçoit sa propre instance de
factory avec sa propre base in-memory. Les classes sont donc totalement indépendantes.
Au sein d'une même classe, les tests partagent la même base (les données s'accumulent),
ce qui est sans incidence car les assertions sont positionnelles (IDs seedés 1–4 + IDs
auto-incrémentés > 4).

---

## Lacunes connues (non testées)

| Cas | Raison |
|---|---|
| `Name` null ou vide | Aucune validation dans l'API — renverrait probablement une erreur 500 (contrainte EF) |
| Prix négatif | Aucune validation — accepté silencieusement (retourne 201) |
| Corps JSON malformé | La minimal API renvoie 400 via le model binder ; non couvert |
| Pagination / grand volume | Hors scope — aucun endpoint paginé n'existe |

> La décision d'ajouter ou non ces tests appartient à l'équipe.

---

## Comment reproduire

```bash
# Exécuter uniquement les tests Catalog
dotnet test tests/ECommerce.Catalog.Tests

# Exécuter tous les tests de la solution
dotnet test ECommerce.slnx

# Avec couverture de code
dotnet test tests/ECommerce.Catalog.Tests --collect:"XPlat Code Coverage"
```
