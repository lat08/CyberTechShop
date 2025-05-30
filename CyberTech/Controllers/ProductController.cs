using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CyberTech.Data;
using CyberTech.Models;
using CyberTech.ViewModels;

namespace CyberTech.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hiển thị sản phẩm theo category
        public async Task<IActionResult> Category(string categoryId, ProductFilterModel filter)
        {
            var category = await _context.Categories
                .Include(c => c.Subcategories)
                    .ThenInclude(s => s.SubSubcategories)
                .FirstOrDefaultAsync(c => c.CategoryID.ToString() == categoryId);

            if (category == null)
                return NotFound();

            filter.CategoryId = categoryId;
            var viewModel = await BuildProductListViewModel(filter);
            viewModel.CategoryName = category.Name;
            viewModel.BreadcrumbPath = category.Name;

            return View("ProductList", viewModel);
        }

        // Hiển thị sản phẩm theo subcategory
        public async Task<IActionResult> Subcategory(string subcategoryId, ProductFilterModel filter)
        {
            var subcategory = await _context.Subcategories
                .Include(s => s.Category)
                .Include(s => s.SubSubcategories)
                .FirstOrDefaultAsync(s => s.SubcategoryID.ToString() == subcategoryId);

            if (subcategory == null)
                return NotFound();

            filter.SubcategoryId = subcategoryId;
            var viewModel = await BuildProductListViewModel(filter);
            viewModel.CategoryName = subcategory.Name;
            viewModel.BreadcrumbPath = $"{subcategory.Category.Name} / {subcategory.Name}";

            return View("ProductList", viewModel);
        }

        // Hiển thị sản phẩm theo subsubcategory
        public async Task<IActionResult> SubSubcategory(string subSubcategoryId, ProductFilterModel filter)
        {
            var subSubcategory = await _context.SubSubcategories
                .Include(ss => ss.Subcategory)
                    .ThenInclude(s => s.Category)
                .FirstOrDefaultAsync(ss => ss.SubSubcategoryID.ToString() == subSubcategoryId);

            if (subSubcategory == null)
                return NotFound();

            filter.SubSubcategoryId = subSubcategoryId;
            var viewModel = await BuildProductListViewModel(filter);
            viewModel.CategoryName = subSubcategory.Name;
            viewModel.BreadcrumbPath = $"{subSubcategory.Subcategory.Category.Name} / {subSubcategory.Subcategory.Name} / {subSubcategory.Name}";

            return View("ProductList", viewModel);
        }

        // Tìm kiếm sản phẩm
        public async Task<IActionResult> Search(ProductFilterModel filter)
        {
            if (string.IsNullOrWhiteSpace(filter.SearchQuery))
            {
                return RedirectToAction("Index", "Product");
            }

            var viewModel = await BuildSearchViewModel(filter);
            viewModel.CategoryName = $"Tìm kiếm: {filter.SearchQuery}";
            viewModel.BreadcrumbPath = "Kết quả tìm kiếm";

            return View("Search", viewModel);
        }

        private async Task<ProductListViewModel> BuildProductListViewModel(ProductFilterModel filter)
        {
            // Debug: Log filter attributes
            if (filter.Attributes != null && filter.Attributes.Any())
            {
                Console.WriteLine($"Filter Attributes Count: {filter.Attributes.Count}");
                foreach (var attr in filter.Attributes)
                {
                    Console.WriteLine($"Attribute: {attr.Key} = {attr.Value}");
                }
            }
            else
            {
                Console.WriteLine("No filter attributes found - checking query parameters directly");
                
                // Read attributes from query parameters directly
                var queryParams = Request.Query;
                var knownAttributeNames = new[] { "RAM", "CPU", "SSD", "Graphics Card", "Display", "OS", "LED RGB", "Kết nối" };
                
                foreach (var attrName in knownAttributeNames)
                {
                    if (queryParams.ContainsKey(attrName))
                    {
                        var attrValue = queryParams[attrName].ToString();
                        if (!string.IsNullOrEmpty(attrValue))
                        {
                            Console.WriteLine($"Found attribute in query: {attrName} = {attrValue}");
                            filter.Attributes[attrName] = attrValue;
                        }
                    }
                }
                
                if (filter.Attributes.Any())
                {
                    Console.WriteLine($"Added {filter.Attributes.Count} attributes from query parameters");
                }
            }

            var query = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductAttributeValues)
                    .ThenInclude(pav => pav.AttributeValue)
                        .ThenInclude(av => av.ProductAttribute)
                .Include(p => p.Reviews)
                .Include(p => p.SubSubcategory)
                    .ThenInclude(ss => ss.Subcategory)
                        .ThenInclude(s => s.Category)
                .Where(p => p.Status == "Active");

            // Lọc theo danh mục
            if (!string.IsNullOrEmpty(filter.CategoryId))
            {
                query = query.Where(p => p.SubSubcategory.Subcategory.CategoryID.ToString() == filter.CategoryId);
            }

            if (!string.IsNullOrEmpty(filter.SubcategoryId))
            {
                query = query.Where(p => p.SubSubcategory.SubcategoryID.ToString() == filter.SubcategoryId);
            }

            if (!string.IsNullOrEmpty(filter.SubSubcategoryId))
            {
                query = query.Where(p => p.SubSubcategoryID.ToString() == filter.SubSubcategoryId);
            }

            // Lọc theo tìm kiếm
            if (!string.IsNullOrEmpty(filter.SearchQuery))
            {
                query = query.Where(p => p.Name.Contains(filter.SearchQuery) || 
                                        p.Description.Contains(filter.SearchQuery) ||
                                        p.Brand.Contains(filter.SearchQuery));
            }

            // Lọc theo giá
            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p => (p.SalePrice != null ? p.SalePrice.Value : p.Price) >= filter.MinPrice.Value);
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => (p.SalePrice != null ? p.SalePrice.Value : p.Price) <= filter.MaxPrice.Value);
            }

            // Lọc theo khuyến mãi
            if (filter.HasDiscount == true)
            {
                query = query.Where(p => p.SalePrice.HasValue && p.SalePrice < p.Price);
            }

            // Lọc theo thuộc tính
            foreach (var attr in filter.Attributes)
            {
                var attributeKey = attr.Key?.Trim();
                var attributeValue = attr.Value?.Trim();
                
                Console.WriteLine($"Filtering by attribute: '{attributeKey}' = '{attributeValue}'");
                
                var beforeCount = await query.CountAsync();
                Console.WriteLine($"Products before filtering by {attributeKey}: {beforeCount}");
                
                query = query.Where(p => p.ProductAttributeValues.Any(pav => 
                    pav.AttributeValue.ProductAttribute.AttributeName.Trim() == attributeKey && 
                    pav.AttributeValue.ValueName.Trim() == attributeValue));
                    
                var afterCount = await query.CountAsync();
                Console.WriteLine($"Products after filtering by {attributeKey}: {afterCount}");
                
                // Debug: Check what values exist for this attribute
                var existingValues = await _context.ProductAttributeValues
                    .Include(pav => pav.AttributeValue)
                        .ThenInclude(av => av.ProductAttribute)
                    .Where(pav => pav.AttributeValue.ProductAttribute.AttributeName.Trim() == attributeKey)
                    .Select(pav => new { 
                        AttributeName = pav.AttributeValue.ProductAttribute.AttributeName,
                        ValueName = pav.AttributeValue.ValueName,
                        ProductId = pav.ProductID
                    })
                    .Distinct()
                    .ToListAsync();
                    
                Console.WriteLine($"Existing values for '{attributeKey}':");
                foreach (var ev in existingValues.Take(10)) // Limit to first 10 for readability
                {
                    Console.WriteLine($"  - '{ev.ValueName}' (Product: {ev.ProductId})");
                }
            }

            // Sắp xếp
            query = filter.SortBy?.ToLower() switch
            {
                "price" => filter.SortOrder == "desc" ? query.OrderByDescending(p => p.SalePrice != null ? p.SalePrice.Value : p.Price) : query.OrderBy(p => p.SalePrice != null ? p.SalePrice.Value : p.Price),
                "name" => filter.SortOrder == "desc" ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                "rating" => query.OrderByDescending(p => p.Reviews.Average(r => (double?)r.Rating) ?? 0),
                "bestseller" => query.OrderByDescending(p => p.OrderItems.Sum(oi => oi.Quantity)),
                _ => query.OrderBy(p => p.Name)
            };

            var totalProducts = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalProducts / filter.PageSize);

            var products = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // Lấy category ID hiện tại để load attributes
            int? currentCategoryId = null;
            if (!string.IsNullOrEmpty(filter.CategoryId))
            {
                currentCategoryId = int.Parse(filter.CategoryId);
            }
            else if (!string.IsNullOrEmpty(filter.SubcategoryId))
            {
                var subcategory = await _context.Subcategories.FindAsync(int.Parse(filter.SubcategoryId));
                currentCategoryId = subcategory?.CategoryID;
            }
            else if (!string.IsNullOrEmpty(filter.SubSubcategoryId))
            {
                var subSubcategory = await _context.SubSubcategories
                    .Include(ss => ss.Subcategory)
                    .FirstOrDefaultAsync(ss => ss.SubSubcategoryID == int.Parse(filter.SubSubcategoryId));
                currentCategoryId = subSubcategory?.Subcategory.CategoryID;
            }

            // Load CategoryAttributes cho category hiện tại
            var categoryAttributes = new List<CategoryAttributeFilter>();
            if (currentCategoryId.HasValue)
            {
                var categoryAttrs = await _context.CategoryAttributes
                    .Where(ca => ca.CategoryID == currentCategoryId.Value)
                    .ToListAsync();

                foreach (var categoryAttr in categoryAttrs)
                {
                    // Lấy tất cả values cho attribute này từ các sản phẩm trong category
                    var attributeValues = await _context.ProductAttributeValues
                        .Include(pav => pav.AttributeValue)
                            .ThenInclude(av => av.ProductAttribute)
                        .Include(pav => pav.Product)
                            .ThenInclude(p => p.SubSubcategory)
                                .ThenInclude(ss => ss.Subcategory)
                        .Where(pav => pav.AttributeValue.ProductAttribute.AttributeName == categoryAttr.AttributeName &&
                                     pav.Product.SubSubcategory.Subcategory.CategoryID == currentCategoryId.Value &&
                                     pav.Product.Status == "Active")
                        .ToListAsync();

                    var groupedValues = attributeValues
                        .GroupBy(pav => pav.AttributeValue.ValueName)
                        .Select(g => new AttributeValueOption
                        {
                            Value = g.Key,
                            DisplayText = g.Key,
                            ProductCount = g.Select(pav => pav.Product.ProductID).Distinct().Count(), // Count unique products
                            IsSelected = filter.Attributes.ContainsKey(categoryAttr.AttributeName) && 
                                        filter.Attributes[categoryAttr.AttributeName] == g.Key
                        })
                        .Where(avo => avo.ProductCount > 0) // Only include values that have products
                        .OrderBy(avo => avo.DisplayText)
                        .ToList();

                    if (groupedValues.Any())
                    {
                        categoryAttributes.Add(new CategoryAttributeFilter
                        {
                            AttributeName = categoryAttr.AttributeName,
                            DisplayName = GetDisplayName(categoryAttr.AttributeName),
                            Values = groupedValues
                        });
                    }
                }
            }

            // Lấy giá min/max
            var allFilteredProducts = await _context.Products
                .Include(p => p.SubSubcategory)
                    .ThenInclude(ss => ss.Subcategory)
                .Where(p => p.Status == "Active")
                .Where(p => currentCategoryId == null || p.SubSubcategory.Subcategory.CategoryID == currentCategoryId)
                .ToListAsync();

            var priceRange = allFilteredProducts.Any() ? 
                new { Min = allFilteredProducts.Min(p => p.SalePrice != null ? p.SalePrice.Value : p.Price), Max = allFilteredProducts.Max(p => p.SalePrice != null ? p.SalePrice.Value : p.Price) } : 
                new { Min = 0m, Max = 0m };

            // Tạo ProductViewModel
            var productViewModels = products.Select(p => new ProductViewModel
            {
                ProductID = p.ProductID,
                Name = p.Name ?? "",
                Price = p.Price,
                SalePrice = p.SalePrice.HasValue ? p.SalePrice.Value : (decimal?)null,
                SalePercentage = p.SalePercentage,
                DiscountedPrice = p.SalePrice,
                PrimaryImageUrl = p.ProductImages.FirstOrDefault()?.ImageURL ?? "/images/default-product.jpg",
                PrimaryImageUrlSmall = p.ProductImages.FirstOrDefault()?.ImageURL ?? "/images/default-product.jpg",
                Url = Url.Action("ProductDetail", "Product", new { id = p.ProductID }),
                Attributes = p.ProductAttributeValues.ToDictionary(
                    pav => pav.AttributeValue.ProductAttribute.AttributeName,
                    pav => pav.AttributeValue.ValueName
                ),
                AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
                ReviewCount = p.Reviews.Count(),
                Brand = p.Brand ?? "",
                Status = p.Status ?? "Active",
                SubSubcategory = p.SubSubcategory,
                IsInStock = p.Stock > 0
            }).ToList();

            return new ProductListViewModel
            {
                Products = productViewModels,
                Filter = filter,
                TotalProducts = totalProducts,
                TotalPages = totalPages,
                CurrentPage = filter.Page,
                CategoryAttributes = categoryAttributes,
                MinPrice = priceRange.Min,
                MaxPrice = priceRange.Max
            };
        }

        private decimal? GetDiscountedPrice(Product product)
        {
            var discountPriceAttr = product.ProductAttributeValues
                .FirstOrDefault(pav => pav.AttributeValue.ProductAttribute.AttributeName == "DiscountPrice");
            
            if (discountPriceAttr != null && decimal.TryParse(discountPriceAttr.AttributeValue.ValueName, out decimal discountPrice))
            {
                return discountPrice;
            }
            
            return null;
        }

        private string GetDisplayName(string attributeName)
        {
            return attributeName switch
            {
                "CPU" => "Bộ vi xử lý",
                "RAM" => "Bộ nhớ RAM",
                "SSD" => "Ổ cứng SSD",
                "Graphics Card" => "Card đồ họa",
                "Display" => "Màn hình",
                "OS" => "Hệ điều hành",
                "LED RGB" => "Đèn LED RGB",
                "Kết nối" => "Kết nối",
                _ => attributeName
            };
        }

        public async Task<IActionResult> ProductDetail(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductAttributeValues)
                    .ThenInclude(pav => pav.AttributeValue)
                        .ThenInclude(av => av.ProductAttribute)
                .Include(p => p.Reviews)
                .Include(p => p.SubSubcategory)
                    .ThenInclude(ss => ss.Subcategory)
                        .ThenInclude(s => s.Category)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null)
            {
                return NotFound();
            }

            // Lấy sản phẩm liên quan
            var relatedProducts = await _context.Products
                .Where(p => p.SubSubcategoryID == product.SubSubcategoryID && p.ProductID != id)
                .Take(4)
                .ToListAsync();

            var viewModel = new ProductDetailViewModel
            {
                Product = product,
                RelatedProducts = relatedProducts
            };

            return View(viewModel);
        }

        public async Task<IActionResult> DebugBinding(ProductFilterModel filter)
        {
            var debugInfo = new
            {
                CategoryId = filter.CategoryId,
                SubcategoryId = filter.SubcategoryId,
                SubSubcategoryId = filter.SubSubcategoryId,
                MinPrice = filter.MinPrice,
                MaxPrice = filter.MaxPrice,
                HasDiscount = filter.HasDiscount,
                SearchQuery = filter.SearchQuery,
                AttributesCount = filter.Attributes?.Count ?? 0,
                Attributes = filter.Attributes?.ToDictionary(a => a.Key, a => a.Value) ?? new Dictionary<string, string>(),
                RawQueryString = Request.QueryString.Value
            };

            return Json(debugInfo);
        }

        public async Task<IActionResult> DebugAttributes(int categoryId = 1)
        {
            var categoryAttributes = await _context.CategoryAttributes
                .Where(ca => ca.CategoryID == categoryId)
                .ToListAsync();

            var result = new List<object>();
            
            foreach (var categoryAttr in categoryAttributes)
            {
                var attributeValues = await _context.ProductAttributeValues
                    .Include(pav => pav.AttributeValue)
                        .ThenInclude(av => av.ProductAttribute)
                    .Include(pav => pav.Product)
                        .ThenInclude(p => p.SubSubcategory)
                            .ThenInclude(ss => ss.Subcategory)
                    .Where(pav => pav.AttributeValue.ProductAttribute.AttributeName == categoryAttr.AttributeName &&
                                 pav.Product.SubSubcategory.Subcategory.CategoryID == categoryId &&
                                 pav.Product.Status == "Active")
                    .Select(pav => new {
                        AttributeName = pav.AttributeValue.ProductAttribute.AttributeName,
                        ValueName = pav.AttributeValue.ValueName,
                        ProductId = pav.Product.ProductID,
                        ProductName = pav.Product.Name
                    })
                    .ToListAsync();

                result.Add(new {
                    AttributeName = categoryAttr.AttributeName,
                    Values = attributeValues.GroupBy(av => av.ValueName)
                        .Select(g => new {
                            Value = g.Key,
                            Count = g.Count(),
                            Products = g.Select(x => new { x.ProductId, x.ProductName }).ToList()
                        }).ToList()
                });
            }

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> SearchSuggestions(string query, int limit = 15)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return Json(new { success = false, message = "Query too short" });
                }

                var products = await _context.Products
                    .Where(p => p.Status == "Active" && 
                               p.Stock > 0 &&
                               p.Name.Contains(query))
                    .OrderByDescending(p => p.Name.StartsWith(query) ? 1 : 0)
                    .ThenBy(p => p.Name)
                    .Take(limit)
                    .ToListAsync();

                var suggestions = new List<object>();
                
                foreach (var product in products)
                {
                    var primaryImage = await _context.ProductImages
                        .Where(pi => pi.ProductID == product.ProductID && pi.IsPrimary)
                        .FirstOrDefaultAsync();
                    
                    var imageUrl = primaryImage?.ImageURL ?? "/images/no-image.png";
                    
                    // Create simple object with explicit properties
                    var suggestion = new
                    {
                        id = product.ProductID,
                        name = product.Name,
                        price = product.Price,
                        salePrice = product.SalePrice,
                        salePercentage = product.SalePercentage,
                        image = imageUrl,
                        // Computed properties từ logic mới với Round Down
                        currentPrice = product.SalePrice.HasValue && product.SalePrice > 0 
                                      ? product.SalePrice.Value 
                                      : (product.SalePercentage.HasValue && product.SalePercentage > 0 
                                         ? Math.Floor((product.Price * (1 - product.SalePercentage.Value / 100)) / 1000) * 1000
                                         : product.Price),
                        hasSale = (product.SalePrice.HasValue && product.SalePrice > 0 && product.SalePrice < product.Price) ||
                                 (product.SalePercentage.HasValue && product.SalePercentage > 0)
                    };
                    
                    suggestions.Add(suggestion);
                }

                return Json(new { success = true, data = suggestions });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<ProductListViewModel> BuildSearchViewModel(ProductFilterModel filter)
        {
            var query = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductAttributeValues)
                    .ThenInclude(pav => pav.AttributeValue)
                        .ThenInclude(av => av.ProductAttribute)
                .Include(p => p.Reviews)
                .Include(p => p.SubSubcategory)
                    .ThenInclude(ss => ss.Subcategory)
                        .ThenInclude(s => s.Category)
                .Where(p => p.Status == "Active");

            // Search by main query
            if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
            {
                var searchTerms = filter.SearchQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in searchTerms)
                {
                    query = query.Where(p => p.Name.Contains(term) || 
                                           p.Description.Contains(term) ||
                                           p.Brand.Contains(term));
                }
            }

            // Refine search
            if (!string.IsNullOrWhiteSpace(filter.RefineQuery))
            {
                query = query.Where(p => p.Name.Contains(filter.RefineQuery) || 
                                       p.Description.Contains(filter.RefineQuery));
            }

            // Filter by categories
            if (filter.Categories != null && filter.Categories.Any())
            {
                var categoryIds = filter.Categories.Select(int.Parse).ToList();
                query = query.Where(p => categoryIds.Contains(p.SubSubcategory.Subcategory.CategoryID));
            }

            // Filter by brands
            if (filter.Brands != null && filter.Brands.Any())
            {
                query = query.Where(p => filter.Brands.Contains(p.Brand));
            }

            // Price filter
            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p => (p.SalePrice != null ? p.SalePrice.Value : p.Price) >= filter.MinPrice.Value);
            }
            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => (p.SalePrice != null ? p.SalePrice.Value : p.Price) <= filter.MaxPrice.Value);
            }

            // Discount filter
            if (filter.HasDiscount == true)
            {
                query = query.Where(p => p.SalePrice.HasValue && p.SalePrice < p.Price);
            }

            // Stock filter
            if (filter.InStock == true)
            {
                query = query.Where(p => p.Stock > 0);
            }

            // Sorting
            query = filter.SortBy?.ToLower() switch
            {
                "relevance" => query.OrderByDescending(p => p.Name.StartsWith(filter.SearchQuery != null ? filter.SearchQuery : "") ? 2 : 
                                                           p.Name.Contains(filter.SearchQuery != null ? filter.SearchQuery : "") ? 1 : 0)
                                   .ThenBy(p => p.Name),
                "price" when filter.SortOrder == "desc" => query.OrderByDescending(p => p.SalePrice != null ? p.SalePrice.Value : p.Price),
                "price" => query.OrderBy(p => p.SalePrice != null ? p.SalePrice.Value : p.Price),
                "name" when filter.SortOrder == "desc" => query.OrderByDescending(p => p.Name),
                "name" => query.OrderBy(p => p.Name),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                "rating" => query.OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0),
                "bestseller" => query.OrderByDescending(p => p.OrderItems.Sum(oi => oi.Quantity)),
                _ => query.OrderByDescending(p => p.Name.StartsWith(filter.SearchQuery != null ? filter.SearchQuery : "") ? 2 : 
                                           p.Name.Contains(filter.SearchQuery != null ? filter.SearchQuery : "") ? 1 : 0)
                           .ThenBy(p => p.Name)
            };

            var totalProducts = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalProducts / (double)filter.PageSize);

            // Get price range for filters
            var priceRange = await query
                .Select(p => p.SalePrice != null ? p.SalePrice.Value : p.Price)
                .DefaultIfEmpty()
                .GroupBy(x => 1)
                .Select(g => new { Min = g.Min(), Max = g.Max() })
                .FirstOrDefaultAsync() ?? new { Min = 0m, Max = 0m };

            var products = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // Build category attributes for filters (from search results)
            var categoryAttributes = await BuildSearchCategoryAttributes(query, filter);

            // Create ProductViewModels
            var productViewModels = products.Select(p => new ProductViewModel
            {
                ProductID = p.ProductID,
                Name = p.Name ?? "",
                Price = p.Price,
                SalePrice = p.SalePrice.HasValue ? p.SalePrice.Value : (decimal?)null,
                SalePercentage = p.SalePercentage,
                DiscountedPrice = p.SalePrice,
                PrimaryImageUrl = p.ProductImages.FirstOrDefault(pi => pi.IsPrimary)?.ImageURL ?? 
                                p.ProductImages.FirstOrDefault()?.ImageURL ?? "/images/no-image.png",
                PrimaryImageUrlSmall = p.ProductImages.FirstOrDefault(pi => pi.IsPrimary)?.ImageURL ?? 
                                     p.ProductImages.FirstOrDefault()?.ImageURL ?? "/images/no-image.png",
                Url = Url.Action("ProductDetail", "Product", new { id = p.ProductID }),
                Attributes = p.ProductAttributeValues.ToDictionary(
                    pav => pav.AttributeValue.ProductAttribute.AttributeName,
                    pav => pav.AttributeValue.ValueName
                ),
                AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
                ReviewCount = p.Reviews.Count(),
                Brand = p.Brand ?? "",
                Status = p.Status ?? "Active",
                SubSubcategory = p.SubSubcategory,
                IsInStock = p.Stock > 0
            }).ToList();

            return new ProductListViewModel
            {
                Products = productViewModels,
                Filter = filter,
                TotalProducts = totalProducts,
                TotalPages = totalPages,
                CurrentPage = filter.Page,
                CategoryAttributes = categoryAttributes,
                MinPrice = priceRange.Min,
                MaxPrice = priceRange.Max
            };
        }

        private async Task<List<CategoryAttributeFilter>> BuildSearchCategoryAttributes(IQueryable<Product> searchQuery, ProductFilterModel filter)
        {
            var categoryAttributes = new List<CategoryAttributeFilter>();

            // Get categories from search results
            var categories = await searchQuery
                .Select(p => new { 
                    p.SubSubcategory.Subcategory.Category.CategoryID, 
                    p.SubSubcategory.Subcategory.Category.Name 
                })
                .Distinct()
                .GroupBy(c => new { c.CategoryID, c.Name })
                .Select(g => new AttributeValueOption
                {
                    Value = g.Key.CategoryID.ToString(),
                    DisplayText = g.Key.Name,
                    ProductCount = g.Count(),
                    IsSelected = filter.Categories != null && filter.Categories.Contains(g.Key.CategoryID.ToString())
                })
                .ToListAsync();

            if (categories.Any())
            {
                categoryAttributes.Add(new CategoryAttributeFilter
                {
                    AttributeName = "Category",
                    DisplayName = "Danh mục",
                    Values = categories.OrderBy(c => c.DisplayText)
                });
            }

            // Get brands from search results
            var brands = await searchQuery
                .Where(p => !string.IsNullOrEmpty(p.Brand))
                .GroupBy(p => p.Brand)
                .Select(g => new AttributeValueOption
                {
                    Value = g.Key,
                    DisplayText = g.Key,
                    ProductCount = g.Count(),
                    IsSelected = filter.Brands != null && filter.Brands.Contains(g.Key)
                })
                .OrderBy(b => b.DisplayText)
                .ToListAsync();

            if (brands.Any())
            {
                categoryAttributes.Add(new CategoryAttributeFilter
                {
                    AttributeName = "Brand",
                    DisplayName = "Thương hiệu",
                    Values = brands
                });
            }

            return categoryAttributes;
        }
    }
}
