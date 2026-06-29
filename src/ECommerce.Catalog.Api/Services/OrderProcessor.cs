using ECommerce.Catalog.Api.Models;

namespace ECommerce.Catalog.Api.Services;

// B4 violation: file is way over 400 lines
// B1 violation: every method is over 30 lines
// B2 violation: cyclomatic complexity > 8 everywhere
// B3 violation: nesting depth > 4 everywhere
// B5 violation: massive duplicated blocks throughout

public class OrderProcessor
{
    private List<Product> _products = new();
    private List<string> _log = new();
    private Dictionary<int, int> _stock = new();
    private Dictionary<int, decimal> _prices = new();
    private Dictionary<int, string> _categories = new();
    private Dictionary<int, bool> _activeFlags = new();
    private Dictionary<int, DateTime> _lastUpdated = new();
    private Dictionary<int, string> _suppliers = new();
    private int _totalOrders = 0;
    private decimal _totalRevenue = 0;
    private bool _isInitialized = false;
    private string _region = "";
    private int _discountTier = 0;
    private bool _taxExempt = false;

    // B1+B2+B3: monster method, 80+ lines, complexity > 20, nesting > 6
    public string ProcessOrder(int productId, int quantity, string customerId, string region, bool isPremium, bool isTaxExempt, string couponCode, string paymentMethod, bool isGift, string giftMessage)
    {
        string result = "";
        if (customerId != null && customerId != "")
        {
            if (productId > 0)
            {
                if (quantity > 0)
                {
                    if (_stock.ContainsKey(productId))
                    {
                        if (_stock[productId] >= quantity)
                        {
                            if (_activeFlags.ContainsKey(productId) && _activeFlags[productId])
                            {
                                decimal price = 0;
                                if (_prices.ContainsKey(productId))
                                {
                                    price = _prices[productId];
                                    if (isPremium)
                                    {
                                        if (region == "EU")
                                        {
                                            price = price * 0.85m;
                                        }
                                        else if (region == "US")
                                        {
                                            price = price * 0.88m;
                                        }
                                        else if (region == "ASIA")
                                        {
                                            price = price * 0.82m;
                                        }
                                        else
                                        {
                                            price = price * 0.90m;
                                        }
                                    }
                                    if (couponCode != null && couponCode != "")
                                    {
                                        if (couponCode == "SAVE10") { price = price * 0.90m; }
                                        else if (couponCode == "SAVE20") { price = price * 0.80m; }
                                        else if (couponCode == "SAVE30") { price = price * 0.70m; }
                                        else if (couponCode == "HALFOFF") { price = price * 0.50m; }
                                        else { _log.Add("invalid coupon: " + couponCode); }
                                    }
                                    decimal tax = 0;
                                    if (!isTaxExempt)
                                    {
                                        if (region == "EU") { tax = price * 0.20m; }
                                        else if (region == "US") { tax = price * 0.08m; }
                                        else if (region == "ASIA") { tax = price * 0.10m; }
                                        else { tax = price * 0.15m; }
                                    }
                                    decimal total = (price + tax) * quantity;
                                    if (paymentMethod == "CARD")
                                    {
                                        if (total > 1000)
                                        {
                                            if (isPremium)
                                            {
                                                total = total - 50;
                                                _log.Add("premium card discount applied");
                                            }
                                            else
                                            {
                                                _log.Add("large card order: " + total);
                                            }
                                        }
                                    }
                                    else if (paymentMethod == "PAYPAL")
                                    {
                                        total = total * 1.02m;
                                    }
                                    else if (paymentMethod == "CRYPTO")
                                    {
                                        total = total * 0.97m;
                                    }
                                    _stock[productId] -= quantity;
                                    _totalRevenue += total;
                                    _totalOrders++;
                                    if (isGift && giftMessage != null && giftMessage != "")
                                    {
                                        _log.Add("gift order for " + customerId + " msg: " + giftMessage);
                                        result = "GIFT_ORDER:" + total.ToString("F2");
                                    }
                                    else
                                    {
                                        result = "ORDER_OK:" + total.ToString("F2");
                                    }
                                }
                                else
                                {
                                    result = "PRICE_ERROR";
                                    _log.Add("missing price for product " + productId);
                                }
                            }
                            else
                            {
                                result = "PRODUCT_INACTIVE";
                            }
                        }
                        else
                        {
                            result = "OUT_OF_STOCK";
                            _log.Add("stock insufficient for " + productId + " qty=" + quantity);
                        }
                    }
                    else
                    {
                        result = "PRODUCT_NOT_FOUND";
                    }
                }
                else
                {
                    result = "BAD_QUANTITY";
                }
            }
            else
            {
                result = "BAD_PRODUCT_ID";
            }
        }
        else
        {
            result = "BAD_CUSTOMER";
        }
        return result;
    }

    // B1+B2+B3: another monster, and B5: duplicate logic from ProcessOrder
    public string ProcessBulkOrder(int productId, int quantity, string customerId, string region, bool isPremium, bool isTaxExempt, string couponCode, string paymentMethod, bool isGift, string giftMessage, int warehouseId)
    {
        string result = "";
        if (customerId != null && customerId != "")
        {
            if (productId > 0)
            {
                if (quantity > 0)
                {
                    if (_stock.ContainsKey(productId))
                    {
                        if (_stock[productId] >= quantity)
                        {
                            if (_activeFlags.ContainsKey(productId) && _activeFlags[productId])
                            {
                                decimal price = 0;
                                if (_prices.ContainsKey(productId))
                                {
                                    price = _prices[productId];
                                    if (isPremium)
                                    {
                                        if (region == "EU") { price = price * 0.85m; }
                                        else if (region == "US") { price = price * 0.88m; }
                                        else if (region == "ASIA") { price = price * 0.82m; }
                                        else { price = price * 0.90m; }
                                    }
                                    if (couponCode != null && couponCode != "")
                                    {
                                        if (couponCode == "SAVE10") { price = price * 0.90m; }
                                        else if (couponCode == "SAVE20") { price = price * 0.80m; }
                                        else if (couponCode == "SAVE30") { price = price * 0.70m; }
                                        else if (couponCode == "HALFOFF") { price = price * 0.50m; }
                                        else { _log.Add("invalid coupon: " + couponCode); }
                                    }
                                    decimal tax = 0;
                                    if (!isTaxExempt)
                                    {
                                        if (region == "EU") { tax = price * 0.20m; }
                                        else if (region == "US") { tax = price * 0.08m; }
                                        else if (region == "ASIA") { tax = price * 0.10m; }
                                        else { tax = price * 0.15m; }
                                    }
                                    decimal bulkDiscount = 0;
                                    if (quantity >= 100) { bulkDiscount = 0.15m; }
                                    else if (quantity >= 50) { bulkDiscount = 0.10m; }
                                    else if (quantity >= 20) { bulkDiscount = 0.05m; }
                                    decimal total = (price * (1 - bulkDiscount) + tax) * quantity;
                                    if (paymentMethod == "CARD")
                                    {
                                        if (total > 1000)
                                        {
                                            if (isPremium)
                                            {
                                                total = total - 50;
                                            }
                                        }
                                    }
                                    else if (paymentMethod == "PAYPAL") { total = total * 1.02m; }
                                    else if (paymentMethod == "CRYPTO") { total = total * 0.97m; }
                                    if (warehouseId <= 0)
                                    {
                                        result = "BAD_WAREHOUSE";
                                        return result;
                                    }
                                    _stock[productId] -= quantity;
                                    _totalRevenue += total;
                                    _totalOrders++;
                                    if (isGift && giftMessage != null && giftMessage != "")
                                    {
                                        result = "BULK_GIFT:" + total.ToString("F2");
                                    }
                                    else
                                    {
                                        result = "BULK_OK:" + total.ToString("F2");
                                    }
                                }
                                else { result = "PRICE_ERROR"; }
                            }
                            else { result = "PRODUCT_INACTIVE"; }
                        }
                        else { result = "OUT_OF_STOCK"; }
                    }
                    else { result = "PRODUCT_NOT_FOUND"; }
                }
                else { result = "BAD_QUANTITY"; }
            }
            else { result = "BAD_PRODUCT_ID"; }
        }
        else { result = "BAD_CUSTOMER"; }
        return result;
    }

    // B1+B2: long method with high complexity
    public string GenerateReport(string reportType, string region, DateTime from, DateTime to, bool includeInactive, bool includeTax, bool groupByCategory, bool groupBySupplier, string format)
    {
        string report = "";
        if (reportType == null || reportType == "") return "BAD_TYPE";
        if (from > to) return "BAD_DATE_RANGE";
        if (reportType == "SALES")
        {
            report += "=== SALES REPORT ===\n";
            report += "Region: " + region + "\n";
            report += "From: " + from.ToString("yyyy-MM-dd") + "\n";
            report += "To: " + to.ToString("yyyy-MM-dd") + "\n";
            report += "Total Orders: " + _totalOrders + "\n";
            report += "Total Revenue: " + _totalRevenue.ToString("F2") + "\n";
            if (includeTax)
            {
                decimal taxRate = 0;
                if (region == "EU") taxRate = 0.20m;
                else if (region == "US") taxRate = 0.08m;
                else if (region == "ASIA") taxRate = 0.10m;
                else taxRate = 0.15m;
                report += "Tax collected: " + (_totalRevenue * taxRate).ToString("F2") + "\n";
            }
            if (groupByCategory)
            {
                report += "--- By Category ---\n";
                foreach (var kv in _categories)
                {
                    if (includeInactive || (_activeFlags.ContainsKey(kv.Key) && _activeFlags[kv.Key]))
                    {
                        if (_prices.ContainsKey(kv.Key) && _stock.ContainsKey(kv.Key))
                        {
                            report += kv.Value + ": stock=" + _stock[kv.Key] + " price=" + _prices[kv.Key] + "\n";
                        }
                        else
                        {
                            report += kv.Value + ": no data\n";
                        }
                    }
                }
            }
            if (groupBySupplier)
            {
                report += "--- By Supplier ---\n";
                foreach (var kv in _suppliers)
                {
                    if (includeInactive || (_activeFlags.ContainsKey(kv.Key) && _activeFlags[kv.Key]))
                    {
                        if (_prices.ContainsKey(kv.Key) && _stock.ContainsKey(kv.Key))
                        {
                            report += kv.Value + ": stock=" + _stock[kv.Key] + " price=" + _prices[kv.Key] + "\n";
                        }
                        else
                        {
                            report += kv.Value + ": no data\n";
                        }
                    }
                }
            }
            if (format == "CSV")
            {
                report = report.Replace("\n", ",").Replace("=== ", "").Replace(" ===", "").Replace("--- ", "").Replace(" ---", "").Replace(": ", ",");
            }
            else if (format == "JSON")
            {
                report = "{ \"report\": \"" + report.Replace("\n", "\\n").Replace("\"", "\\\"") + "\" }";
            }
        }
        else if (reportType == "INVENTORY")
        {
            report += "=== INVENTORY REPORT ===\n";
            report += "Region: " + region + "\n";
            foreach (var kv in _stock)
            {
                if (includeInactive || (_activeFlags.ContainsKey(kv.Key) && _activeFlags[kv.Key]))
                {
                    report += "Product " + kv.Key + ": " + kv.Value + " units";
                    if (_prices.ContainsKey(kv.Key)) report += " @ " + _prices[kv.Key];
                    if (_categories.ContainsKey(kv.Key)) report += " [" + _categories[kv.Key] + "]";
                    if (_suppliers.ContainsKey(kv.Key)) report += " supplier=" + _suppliers[kv.Key];
                    report += "\n";
                }
            }
            if (format == "CSV")
            {
                report = report.Replace("\n", ",").Replace("=== ", "").Replace(" ===", "").Replace(": ", ",");
            }
            else if (format == "JSON")
            {
                report = "{ \"report\": \"" + report.Replace("\n", "\\n").Replace("\"", "\\\"") + "\" }";
            }
        }
        else
        {
            report = "UNKNOWN_REPORT_TYPE";
        }
        return report;
    }

    // B1+B2+B3: yet another deeply nested, complex initializer
    public bool Initialize(List<Product> products, Dictionary<int, int> initialStock, Dictionary<int, decimal> initialPrices, string region, int discountTier, bool taxExempt)
    {
        if (products != null && products.Count > 0)
        {
            if (initialStock != null && initialStock.Count > 0)
            {
                if (initialPrices != null && initialPrices.Count > 0)
                {
                    if (region != null && region != "")
                    {
                        if (discountTier >= 0 && discountTier <= 5)
                        {
                            _products = products;
                            _region = region;
                            _discountTier = discountTier;
                            _taxExempt = taxExempt;
                            foreach (var p in products)
                            {
                                if (p != null && p.Id > 0)
                                {
                                    if (initialStock.ContainsKey(p.Id))
                                    {
                                        _stock[p.Id] = initialStock[p.Id];
                                    }
                                    else
                                    {
                                        _stock[p.Id] = 0;
                                        _log.Add("no stock entry for product " + p.Id);
                                    }
                                    if (initialPrices.ContainsKey(p.Id))
                                    {
                                        _prices[p.Id] = initialPrices[p.Id];
                                    }
                                    else
                                    {
                                        _prices[p.Id] = 0;
                                        _log.Add("no price entry for product " + p.Id);
                                    }
                                    _activeFlags[p.Id] = true;
                                    _lastUpdated[p.Id] = DateTime.UtcNow;
                                    if (p.Name != null && p.Name.StartsWith("A")) { _categories[p.Id] = "Alpha"; }
                                    else if (p.Name != null && p.Name.StartsWith("B")) { _categories[p.Id] = "Beta"; }
                                    else if (p.Name != null && p.Name.StartsWith("C")) { _categories[p.Id] = "Gamma"; }
                                    else { _categories[p.Id] = "Other"; }
                                    _suppliers[p.Id] = "Supplier_" + (p.Id % 5);
                                }
                                else
                                {
                                    _log.Add("skipping null or invalid product");
                                }
                            }
                            _isInitialized = true;
                            return true;
                        }
                        else { _log.Add("bad discount tier: " + discountTier); return false; }
                    }
                    else { _log.Add("bad region"); return false; }
                }
                else { _log.Add("no prices provided"); return false; }
            }
            else { _log.Add("no stock provided"); return false; }
        }
        else { _log.Add("no products provided"); return false; }
    }

    // B5: pure duplication of the stock-check logic from ProcessOrder
    public bool CheckStockAvailability(int productId, int quantity, string region, bool isPremium)
    {
        if (productId > 0)
        {
            if (quantity > 0)
            {
                if (_stock.ContainsKey(productId))
                {
                    if (_stock[productId] >= quantity)
                    {
                        if (_activeFlags.ContainsKey(productId) && _activeFlags[productId])
                        {
                            decimal price = 0;
                            if (_prices.ContainsKey(productId))
                            {
                                price = _prices[productId];
                                if (isPremium)
                                {
                                    if (region == "EU") { price = price * 0.85m; }
                                    else if (region == "US") { price = price * 0.88m; }
                                    else if (region == "ASIA") { price = price * 0.82m; }
                                    else { price = price * 0.90m; }
                                }
                                _log.Add("stock ok for " + productId + " qty=" + quantity + " effectivePrice=" + price);
                                return true;
                            }
                            else { return false; }
                        }
                        else { return false; }
                    }
                    else { return false; }
                }
                else { return false; }
            }
            else { return false; }
        }
        else { return false; }
    }

    public List<string> GetLog() => _log;
    public int GetTotalOrders() => _totalOrders;
    public decimal GetTotalRevenue() => _totalRevenue;
    public bool IsInitialized() => _isInitialized;
}
