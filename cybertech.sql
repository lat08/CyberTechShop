DROP DATABASE cybertech

CREATE DATABASE cybertech
GO
USE cybertech
GO

-- Drop tables if they exist, in reverse dependency order
DROP TABLE IF EXISTS Review;
DROP TABLE IF EXISTS OrderItem;
DROP TABLE IF EXISTS Payment;
DROP TABLE IF EXISTS Shipping;
DROP TABLE IF EXISTS CartItem;
DROP TABLE IF EXISTS Cart;
DROP TABLE IF EXISTS ProductAttributeValue;
DROP TABLE IF EXISTS AttributeValue;
DROP TABLE IF EXISTS Attribute;
DROP TABLE IF EXISTS Promotion_Applicability;
DROP TABLE IF EXISTS ProductImage;
DROP TABLE IF EXISTS Wishlist;
DROP TABLE IF EXISTS Orders;
DROP TABLE IF EXISTS Promotion;
DROP TABLE IF EXISTS Product;
DROP TABLE IF EXISTS SubSubcategory;
DROP TABLE IF EXISTS Subcategory;
DROP TABLE IF EXISTS CategoryAttributes;
DROP TABLE IF EXISTS Category;
DROP TABLE IF EXISTS Customer;
DROP TABLE IF EXISTS Staff;

CREATE TABLE Users
(
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Username VARCHAR(50) NOT NULL UNIQUE,
    Email VARCHAR(100) NOT NULL UNIQUE,
    ProfileImageURL VARCHAR(500) NULL,
    Role VARCHAR(20) NOT NULL CHECK (Role IN ('Customer', 'Support', 'Manager', 'SuperAdmin')),
    Phone VARCHAR(20) NULL,
    Address NVARCHAR(255) NULL,
    Salary DECIMAL(18,2) NULL CHECK (Salary >= 0),
    TotalSpent DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK (TotalSpent >= 0),
    OrderCount INT NOT NULL DEFAULT 0 CHECK (OrderCount >= 0),
    RankId INT NULL,
    EmailVerified BIT NOT NULL DEFAULT 0,
    UserStatus VARCHAR(20) NOT NULL DEFAULT 'Active' CHECK (UserStatus IN ('Active', 'Inactive', 'Suspended')),
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    LastLoginAt DATETIME NULL
);

CREATE TABLE UserAuthMethods
(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    AuthType NVARCHAR(50) NOT NULL CHECK (AuthType IN ('Password', 'Google', 'Facebook')),
    AuthKey NVARCHAR(256) NOT NULL,
    AuthSecret NVARCHAR(500) NULL,
    AuthData NVARCHAR(MAX) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    LastUsedAt DATETIME NULL,
    ExpiresAt DATETIME NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserID) ON DELETE CASCADE,
    CONSTRAINT UQ_UserAuthMethods UNIQUE (UserID, AuthType, AuthKey)
);
CREATE TABLE Rank
(
    RankId INT IDENTITY(1,1) PRIMARY KEY,
    RankName NVARCHAR(50) NOT NULL UNIQUE,
    MinTotalSpent DECIMAL(18,2) NOT NULL CHECK (MinTotalSpent >= 0),
    DiscountPercent DECIMAL(5,2) NULL CHECK (DiscountPercent >= 0),
    PriorityLevel INT NOT NULL CHECK (PriorityLevel >= 0),
    Description NVARCHAR(255) NULL
);

CREATE TABLE UserAddress
(
    AddressID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    AddressLine NVARCHAR(255) NOT NULL,
    City NVARCHAR(100) NULL,
    District NVARCHAR(100) NULL,
    Ward NVARCHAR(100) NULL,
    Phone VARCHAR(20) NULL,
    IsPrimary BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
);
CREATE TABLE PasswordResetTokens
(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    Token NVARCHAR(256) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    ExpiresAt DATETIME NOT NULL,
    Used BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
);

CREATE TABLE Category
(
    CategoryID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description TEXT NULL
);

CREATE TABLE Subcategory
(
    SubcategoryID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description TEXT NULL,
    CategoryID INT NOT NULL,
    FOREIGN KEY (CategoryID) REFERENCES Category(CategoryID)
);

CREATE TABLE SubSubcategory
(
    SubSubcategoryID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description TEXT NULL,
    SubcategoryID INT NOT NULL,
    FOREIGN KEY (SubcategoryID) REFERENCES Subcategory(SubcategoryID) ON DELETE CASCADE
);

CREATE TABLE Product
(
    ProductID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description TEXT NULL,
    Price DECIMAL(10,2) NOT NULL CHECK (Price >= 0),
    Stock INT NOT NULL CHECK (Stock >= 0),
    SubSubcategoryID INT NOT NULL,
    Brand NVARCHAR(100) NULL,
    FOREIGN KEY (SubSubcategoryID) REFERENCES SubSubcategory(SubSubcategoryID)
);

CREATE TABLE ProductImage
(
    ImageID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ProductID INT NOT NULL,
    ImageURL VARCHAR(255) NOT NULL,
    IsPrimary BIT NOT NULL DEFAULT 0,
    DisplayOrder INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (ProductID) REFERENCES Product(ProductID)
);

CREATE TABLE ProductAttribute
(
    AttributeID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    AttributeName VARCHAR(100) NOT NULL,
    AttributeType VARCHAR(50) NOT NULL
);

CREATE TABLE AttributeValue
(
    ValueID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    AttributeID INT NOT NULL,
    ValueName VARCHAR(255) NOT NULL,
    FOREIGN KEY (AttributeID) REFERENCES ProductAttribute(AttributeID) ON DELETE CASCADE
);

CREATE TABLE ProductAttributeValue
(
    ProductID INT NOT NULL,
    ValueID INT NOT NULL,
    PRIMARY KEY (ProductID, ValueID),
    FOREIGN KEY (ProductID) REFERENCES Product(ProductID) ON DELETE CASCADE,
    FOREIGN KEY (ValueID) REFERENCES AttributeValue(ValueID) ON DELETE CASCADE
);

CREATE TABLE Promotion
(
    PromotionID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    DiscountType VARCHAR(20) NOT NULL CHECK (DiscountType IN ('Percentage', 'FixedAmount')),
    DiscountValue DECIMAL(10,2) NOT NULL CHECK (DiscountValue >= 0),
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CHECK (EndDate >= StartDate)
);

CREATE TABLE Promotion_Applicability
(
    PromotionApplicabilityID INT IDENTITY(1,1) PRIMARY KEY,
    PromotionID INT NOT NULL,
    ProductID INT NULL,
    CategoryID INT NULL,
    FOREIGN KEY (PromotionID) REFERENCES Promotion(PromotionID) ON DELETE CASCADE,
    FOREIGN KEY (ProductID) REFERENCES Product(ProductID) ON DELETE CASCADE,
    FOREIGN KEY (CategoryID) REFERENCES Category(CategoryID) ON DELETE CASCADE,
    CHECK ((ProductID IS NOT NULL AND CategoryID IS NULL) OR (ProductID IS NULL AND CategoryID IS NOT NULL))
);

CREATE TABLE Wishlist
(
    WishlistID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserID INT NOT NULL,
    ProductID INT NOT NULL,
    AddedDate DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (ProductID) REFERENCES Product(ProductID),
    CONSTRAINT UK_Wishlist UNIQUE (UserID, ProductID)
);

CREATE TABLE Cart
(
    CartID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserID INT NOT NULL,
    TotalPrice DECIMAL(10,2) NOT NULL DEFAULT 0.00 CHECK (TotalPrice >= 0),
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);

CREATE TABLE CartItem
(
    CartItemID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CartID INT NOT NULL,
    ProductID INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    Subtotal DECIMAL(10,2) NOT NULL CHECK (Subtotal >= 0),
    FOREIGN KEY (CartID) REFERENCES Cart(CartID),
    FOREIGN KEY (ProductID) REFERENCES Product(ProductID)
);

CREATE TABLE Orders
(
    OrderID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserID INT NOT NULL,
    TotalPrice DECIMAL(10,2) NOT NULL DEFAULT 0.00 CHECK (TotalPrice >= 0),
    DiscountAmount DECIMAL(10,2) NULL CHECK (DiscountAmount >= 0),
    FinalPrice DECIMAL(10,2) NOT NULL CHECK (FinalPrice >= 0),
    Status VARCHAR(50) NOT NULL DEFAULT 'Pending'
        CHECK (Status IN ('Pending', 'Processing', 'Shipped', 'Delivered', 'Cancelled')),
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CHECK (FinalPrice = TotalPrice - ISNULL(DiscountAmount, 0))
);

CREATE TABLE OrderItem
(
    OrderItemID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    OrderID INT NOT NULL,
    ProductID INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    PromotionID INT NULL,
    Subtotal DECIMAL(10,2) NOT NULL CHECK (Subtotal >= 0),
    DiscountAmount DECIMAL(10,2) NULL CHECK (DiscountAmount >= 0),
    FinalSubtotal DECIMAL(10,2) NOT NULL CHECK (FinalSubtotal >= 0),
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID),
    FOREIGN KEY (ProductID) REFERENCES Product(ProductID),
    CHECK (FinalSubtotal = Subtotal - ISNULL(DiscountAmount, 0))
);

CREATE TABLE Payment
(
    PaymentID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    OrderID INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL CHECK (Amount >= 0),
    PaymentMethod VARCHAR(50) NOT NULL
        CHECK (PaymentMethod IN ('COD', 'VNPay', 'Momo')),
    PaymentStatus VARCHAR(50) NOT NULL DEFAULT 'Pending'
        CHECK (PaymentStatus IN ('Pending', 'Completed', 'Failed', 'Refunded')),
    PaymentDate DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID)
);

CREATE TABLE Shipping
(
    ShippingID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    OrderID INT NOT NULL,
    Address NVARCHAR(255) NOT NULL,
    Phone VARCHAR(20) NOT NULL,
    ShippingMethod VARCHAR(50) NOT NULL
        CHECK (ShippingMethod IN ('Standard', 'Express')),
    ShippingCost DECIMAL(10,2) NOT NULL CHECK (ShippingCost >= 0),
    Status VARCHAR(50) NOT NULL DEFAULT 'Pending'
        CHECK (Status IN ('Pending', 'Shipped', 'InTransit', 'Delivered')),
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID)
);

CREATE TABLE Review
(
    ReviewID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserID INT NOT NULL,
    ProductID INT NOT NULL,
    Rating INT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Comment TEXT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (ProductID) REFERENCES Product(ProductID)
);
GO

INSERT INTO CustomerRank
    (RankName, MinPoints, Description)
VALUES
    ('Bronze', 0, ''),
    ('Silver', 1000, ''),
    ('Gold', 5000, ''),
    ('Platinum', 10000, ''),
    ('Diamond', 25000, '');

-- Insert data into Category
INSERT INTO Category
    (Name, Description)
VALUES
    ('Laptop', 'Laptops and notebooks'),
    ('Laptop Gaming', 'Gaming laptops'),
    ('PC GVN', 'Pre-built PCs'),
    ('Main, CPU, VGA', 'Motherboards, CPUs, and GPUs'),
    (N'Case, Nguồn, Tản', 'Cases, power supplies, and cooling'),
    (N'Ổ cứng, RAM, Thẻ nhớ', 'Storage and memory'),
    (N'Loa, Micro, Webcam', 'Audio and video peripherals'),
    (N'Màn hình', 'Monitors'),
    (N'Bàn phím', 'Keyboards'),
    (N'Chuột + Lót chuột', 'Mice and mousepads'),
    ('Tai Nghe', 'Headphones'),
    (N'Ghế - Bàn', 'Chairs and desks'),
    (N'Phần mềm, mạng', 'Software and networking'),
    ('Handheld, Console', 'Handheld and console gaming'),
    (N'Phụ kiện (Hub, sạc, cáp..)', 'Accessories'),
    (N'Dịch vụ và thông tin khác', 'Services and other information');
GO

-- Insert data into Subcategory
INSERT INTO Subcategory
    (Name, Description, CategoryID)
VALUES
    -- Category 'Laptop' (CategoryID = 1)
    (N'Thương hiệu', 'Laptop brands', 1),
    (N'Giá bán', 'Price ranges', 1),
    ('CPU Intel - AMD', 'Processor types', 1),
    (N'Nhu cầu sử dụng', 'Usage needs', 1),
    (N'Linh phụ kiện Laptop', 'Laptop accessories', 1),
    ('Laptop ASUS', 'ASUS laptops', 1),
    ('Laptop ACER', 'ACER laptops', 1),
    ('Laptop MSI', 'MSI laptops', 1),
    ('Laptop Lenovo', 'Lenovo laptops', 1),
    ('Laptop Dell', 'Dell laptops', 1),
    ('Laptop AI', 'AI laptops', 1),

    -- Category 'Laptop Gaming' (CategoryID = 2)
    (N'Thương hiệu', 'Gaming laptop brands', 2),
    (N'Giá bán', 'Price ranges', 2),
    ('ACER | PREDATOR', 'ACER gaming series', 2),
    ('ASUS | ROG Gaming', 'ASUS gaming series', 2),
    ('MSI Gaming', 'MSI gaming series', 2),
    ('LENOVO Gaming', 'Lenovo gaming series', 2),
    ('Dell Gaming', 'Dell gaming series', 2),
    ('HP Gaming', 'HP gaming series', 2),
    (N'Cấu hình', 'Configurations', 2),
    (N'Linh - Phụ kiện Laptop', 'Laptop accessories', 2),

    -- Category 'PC GVN' (CategoryID = 3)
    (N'KHUYẾN MÃI HOT', 'Hot promotions', 3),
    (N'PC KHUYẾN MÃI', 'Promotional PCs', 3),
    (N'PC theo cấu hình VGA', 'PCs by VGA configuration', 3),
    ('A.I PC - GVN', 'AI PCs', 3),
    (N'PC theo CPU Intel', 'PCs by Intel CPU', 3),
    (N'PC theo CPU AMD', 'PCs by AMD CPU', 3),
    (N'PC Văn phòng', 'Office PCs', 3),
    (N'Phần mềm bản quyền', 'Licensed software', 3),

    -- Category 'Main, CPU, VGA' (CategoryID = 4)
    ('VGA RTX 50 SERIES', 'RTX 50 series GPUs', 4),
    ('VGA (Trên 12 GB VRAM)', 'GPUs with over 12GB VRAM', 4),
    ('VGA (Dưới 12 GB VRAM)', 'GPUs with under 12GB VRAM', 4),
    ('VGA - Card màn hình', 'Graphics cards', 4),
    (N'Bo mạch chủ Intel', 'Intel motherboards', 4),
    (N'Bo mạch chủ AMD', 'AMD motherboards', 4),
    (N'CPU - Bộ vi xử lý Intel', 'Intel CPUs', 4),
    (N'CPU - Bộ vi xử lý AMD', 'AMD CPUs', 4),

    -- Category 'Case, Nguồn, Tản' (CategoryID = 5)
    ('Case - Theo hãng', 'Cases by brand', 5),
    ('Case - Theo giá', 'Cases by price', 5),
    (N'Nguồn - Theo Hãng', 'Power supplies by brand', 5),
    (N'Nguồn - Theo công suất', 'Power supplies by wattage', 5),
    (N'Phụ kiện PC', 'PC accessories', 5),
    (N'Loại tản nhiệt', 'Cooling types', 5),

    -- Category 'Ổ cứng, RAM, Thẻ nhớ' (CategoryID = 6)
    (N'Dung lượng RAM', 'RAM capacities', 6),
    (N'Loại RAM', 'RAM types', 6),
    (N'Hãng RAM', 'RAM brands', 6),
    (N'Dung lượng HDD', 'HDD capacities', 6),
    (N'Hãng HDD', 'HDD brands', 6),
    (N'Dung lượng SSD', 'SSD capacities', 6),
    (N'Hãng SSD', 'SSD brands', 6),
    (N'Thẻ nhớ / USB', 'Memory cards and USB drives', 6),
    (N'Ổ cứng di động', 'Portable hard drives', 6),

    -- Category 'Loa, Micro, Webcam' (CategoryID = 7)
    (N'Thương hiệu loa', 'Speaker brands', 7),
    (N'Kiểu Loa', 'Speaker types', 7),
    ('Webcam', 'Webcams', 7),
    ('Microphone', 'Microphones', 7),

    -- Category 'Màn hình' (CategoryID = 8)
    (N'Hãng sản xuất', 'Monitor brands', 8),
    (N'Giá tiền', 'Price ranges', 8),
    (N'Độ Phân giải', 'Resolutions', 8),
    (N'Tần số quét', 'Refresh rates', 8),
    (N'Màn hình cong', 'Curved monitors', 8),
    (N'Kích thước', 'Screen sizes', 8),
    (N'Màn hình đồ họa', 'Graphic design monitors', 8),
    (N'Phụ kiện màn hình', 'Monitor accessories', 8),
    (N'Màn hình di động', 'Portable monitors', 8),
    (N'Màn hình Oled', 'OLED monitors', 8),

    -- Category 'Bàn phím' (CategoryID = 9)
    (N'Thương hiệu', 'Keyboard brands', 9),
    (N'Giá tiền', 'Price ranges', 9),
    (N'Kết nối', 'Connection types', 9),
    (N'Phụ kiện bàn phím cơ', 'Mechanical keyboard accessories', 9),

    -- Category 'Chuột + Lót chuột' (CategoryID = 10)
    (N'Thương hiệu chuột', 'Mouse brands', 10),
    (N'Chuột theo giá tiền', 'Mice by price', 10),
    (N'Loại Chuột', 'Mouse types', 10),
    ('Logitech', 'Logitech mice', 10),
    (N'Thương hiệu lót chuột', 'Mousepad brands', 10),
    (N'Các loại lót chuột', 'Mousepad types', 10),
    (N'Lót chuột theo size', 'Mousepad sizes', 10),

    -- Category 'Tai Nghe' (CategoryID = 11)
    (N'Thương hiệu tai nghe', 'Headphone brands', 11),
    (N'Tai nghe theo giá', 'Headphones by price', 11),
    (N'Kiểu kết nối', 'Connection types', 11),
    (N'Kiểu tai nghe', 'Headphone styles', 11),

    -- Category 'Ghế - Bàn' (CategoryID = 12)
    (N'Thương hiệu ghế Gaming', 'Gaming chair brands', 12),
    (N'Thương hiệu ghế CTH', 'Ergonomic chair brands', 12),
    (N'Kiểu ghế', 'Chair types', 12),
    (N'Bàn Gaming', 'Gaming desks', 12),
    (N'Bàn công thái học', 'Ergonomic desks', 12),
    (N'Giá tiền', 'Price ranges', 12),

    -- Category 'Phần mềm, mạng' (CategoryID = 13)
    (N'Hãng sản xuất', 'Manufacturers', 13),
    ('Router Wi-Fi', 'Wi-Fi routers', 13),
    (N'USB Thu sóng - Card mạng', 'Network adapters', 13),
    ('Microsoft Office', 'Microsoft Office software', 13),
    ('Microsoft Windows', 'Microsoft Windows software', 13),

    -- Category 'Handheld, Console' (CategoryID = 14)
    ('Handheld PC', 'Handheld gaming PCs', 14),
    (N'Tay cầm', 'Controllers', 14),
    (N'Vô lăng lái xe, máy bay', 'Steering wheels and flight sticks', 14),
    ('Sony Playstation', 'Sony Playstation consoles', 14),

    -- Category 'Phụ kiện (Hub, sạc, cáp..)' (CategoryID = 15)
    (N'Hub, sạc, cáp', 'Hubs, chargers, and cables', 15),
    (N'Quạt cầm tay, Quạt mini', 'Handheld and mini fans', 15),

    -- Category 'Dịch vụ và thông tin khác' (CategoryID = 16)
    (N'Dịch vụ', 'Services', 16),
    (N'Chính sách', 'Policies', 16),
    ('Build PC', 'PC building services', 16);
GO

-- Insert data into SubSubcategory with dynamic SubcategoryID retrieval
-- 'Laptop' category
INSERT INTO SubSubcategory
    (Name, Description, SubcategoryID)
VALUES
    ('ASUS', 'ASUS laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 1)),
    ('ACER', 'ACER laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 1)),
    ('MSI', 'MSI laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 1)),
    ('LENOVO', 'LENOVO laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 1)),
    ('DELL', 'DELL laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 1)),
    ('HP - Pavilion', 'HP - Pavilion laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 1)),
    ('LG - Gram', 'LG - Gram laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 1)),
    (N'Dưới 15 triệu', 'Laptops under 15 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Giá bán' AND CategoryID = 1)),
    (N'Từ 15 đến 20 triệu', 'Laptops from 15 to 20 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Giá bán' AND CategoryID = 1)),
    (N'Trên 20 triệu', 'Laptops over 20 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Giá bán' AND CategoryID = 1)),
    ('Intel Core i3', 'Laptops with Intel Core i3', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'CPU Intel - AMD' AND CategoryID = 1)),
    ('Intel Core i5', 'Laptops with Intel Core i5', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'CPU Intel - AMD' AND CategoryID = 1)),
    ('Intel Core i7', 'Laptops with Intel Core i7', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'CPU Intel - AMD' AND CategoryID = 1)),
    ('AMD Ryzen', 'Laptops with AMD Ryzen', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'CPU Intel - AMD' AND CategoryID = 1)),
    (N'Đồ họa - Studio', 'Laptops for graphics and studio', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Nhu cầu sử dụng' AND CategoryID = 1)),
    (N'Học sinh - Sinh viên', 'Laptops for students', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Nhu cầu sử dụng' AND CategoryID = 1)),
    (N'Mỏng nhẹ cao cấp', 'Premium thin and light laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Nhu cầu sử dụng' AND CategoryID = 1)),
    ('Ram laptop', 'Laptop RAM', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Linh phụ kiện Laptop' AND CategoryID = 1)),
    ('SSD laptop', 'Laptop SSD', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Linh phụ kiện Laptop' AND CategoryID = 1)),
    (N'Ổ cứng di động', 'Portable hard drives', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Linh phụ kiện Laptop' AND CategoryID = 1)),
    ('ASUS OLED Series', 'ASUS OLED laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Laptop ASUS' AND CategoryID = 1)),
    ('Vivobook Series', 'ASUS Vivobook laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Laptop ASUS' AND CategoryID = 1)),
    ('Zenbook Series', 'ASUS Zenbook laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Laptop ASUS' AND CategoryID = 1)),
    ('Aspire Series', 'ACER Aspire laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Laptop ACER' AND CategoryID = 1)),
    ('Swift Series', 'ACER Swift laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Laptop ACER' AND CategoryID = 1)),
    ('Modern Series', 'MSI Modern laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Laptop MSI' AND CategoryID = 1)),
    ('Prestige Series', 'MSI Prestige laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Laptop MSI' AND CategoryID = 1)),
    ('Thinkbook Series', 'Lenovo Thinkbook laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Laptop Lenovo' AND CategoryID = 1)),
    ('Ideapad Series', 'Lenovo Ideapad laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Laptop Lenovo' AND CategoryID = 1)),
    ('Thinkpad Series', 'Lenovo Thinkpad laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Laptop Lenovo' AND CategoryID = 1)),
    ('Yoga Series', 'Lenovo Yoga laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Laptop Lenovo' AND CategoryID = 1)),
    ('Inspirion Series', 'Dell Inspirion laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Laptop Dell' AND CategoryID = 1)),
    ('Vostro Series', 'Dell Vostro laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Laptop Dell' AND CategoryID = 1)),
    ('Latitude Series', 'Dell Latitude laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Laptop Dell' AND CategoryID = 1)),
    ('XPS Series', 'Dell XPS laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Laptop Dell' AND CategoryID = 1)),
    ('Laptop AI', 'AI laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Laptop AI' AND CategoryID = 1));
GO

-- 'Laptop Gaming' category
INSERT INTO SubSubcategory
    (Name, Description, SubcategoryID)
VALUES
    ('ACER / PREDATOR', 'ACER / PREDATOR gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 2)),
    ('ASUS / ROG', 'ASUS / ROG gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 2)),
    ('MSI', 'MSI gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 2)),
    ('LENOVO', 'LENOVO gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 2)),
    ('DELL', 'DELL gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 2)),
    ('GIGABYTE / AORUS', 'GIGABYTE / AORUS gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 2)),
    ('HP', 'HP gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 2)),
    (N'Dưới 20 triệu', 'Gaming laptops under 20 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Giá bán' AND CategoryID = 2)),
    (N'Từ 20 đến 25 triệu', 'Gaming laptops from 20 to 25 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Giá bán' AND CategoryID = 2)),
    (N'Từ 25 đến 30 triệu', 'Gaming laptops from 25 to 30 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Giá bán' AND CategoryID = 2)),
    (N'Trên 30 triệu', 'Gaming laptops over 30 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Giá bán' AND CategoryID = 2)),
    ('Gaming RTX 50 Series', 'Gaming laptops with RTX 50 Series', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Giá bán' AND CategoryID = 2)),
    ('Nitro Series', 'ACER Nitro gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'ACER | PREDATOR' AND CategoryID = 2)),
    ('Aspire Series', 'ACER Aspire gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'ACER | PREDATOR' AND CategoryID = 2)),
    ('Predator Series', 'ACER Predator gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'ACER | PREDATOR' AND CategoryID = 2)),
    ('ACER RTX 50 Series', 'ACER gaming laptops with RTX 50 Series', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'ACER | PREDATOR' AND CategoryID = 2)),
    ('ROG Series', 'ASUS ROG gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'ASUS | ROG Gaming' AND CategoryID = 2)),
    ('TUF Series', 'ASUS TUF gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'ASUS | ROG Gaming' AND CategoryID = 2)),
    ('Zephyrus Series', 'ASUS Zephyrus gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'ASUS | ROG Gaming' AND CategoryID = 2)),
    ('ASUS RTX 50 Series', 'ASUS gaming laptops with RTX 50 Series', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'ASUS | ROG Gaming' AND CategoryID = 2)),
    ('Titan GT Series', 'MSI Titan GT gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'MSI Gaming' AND CategoryID = 2)),
    ('Stealth GS Series', 'MSI Stealth GS gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'MSI Gaming' AND CategoryID = 2)),
    ('Raider GE Series', 'MSI Raider GE gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'MSI Gaming' AND CategoryID = 2)),
    ('Vector GP Series', 'MSI Vector GP gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'MSI Gaming' AND CategoryID = 2)),
    ('Crosshair / Pulse GL Series', 'MSI Crosshair / Pulse GL gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'MSI Gaming' AND CategoryID = 2)),
    ('Sword / Katana GF66 Series', 'MSI Sword / Katana GF66 gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'MSI Gaming' AND CategoryID = 2)),
    ('Cyborg / Thin GF Series', 'MSI Cyborg / Thin GF gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'MSI Gaming' AND CategoryID = 2)),
    ('MSI RTX 50 Series', 'MSI gaming laptops with RTX 50 Series', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'MSI Gaming' AND CategoryID = 2)),
    ('Legion Gaming', 'LENOVO Legion gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'LENOVO Gaming' AND CategoryID = 2)),
    ('LOQ series', 'LENOVO LOQ gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'LENOVO Gaming' AND CategoryID = 2)),
    ('RTX 50 Series', 'LENOVO gaming laptops with RTX 50 Series', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'LENOVO Gaming' AND CategoryID = 2)),
    ('Dell Gaming G Series', 'Dell Gaming G Series laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Dell Gaming' AND CategoryID = 2)),
    ('Alienware Series', 'Dell Alienware gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Dell Gaming' AND CategoryID = 2)),
    ('HP Victus', 'HP Victus gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'HP Gaming' AND CategoryID = 2)),
    ('Hp Omen', 'HP Omen gaming laptops', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'HP Gaming' AND CategoryID = 2)),
    ('RTX 50 Series', 'Gaming laptops with RTX 50 Series', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Cấu hình' AND CategoryID = 2)),
    ('CPU Core Ultra', 'Gaming laptops with CPU Core Ultra', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Cấu hình' AND CategoryID = 2)),
    ('CPU AMD', 'Gaming laptops with CPU AMD', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Cấu hình' AND CategoryID = 2)),
    ('Ram laptop', 'Laptop RAM', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Linh - Phụ kiện Laptop' AND CategoryID = 2)),
    ('SSD laptop', 'Laptop SSD', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Linh - Phụ kiện Laptop' AND CategoryID = 2)),
    (N'Ổ cứng di động', 'Portable hard drives', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Linh - Phụ kiện Laptop' AND CategoryID = 2));
GO

-- 'PC GVN' category
INSERT INTO SubSubcategory
    (Name, Description, SubcategoryID)
VALUES
    ('PC RTX 5090', 'PCs with RTX 5090', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'KHUYẾN MÃI HOT' AND CategoryID = 3)),
    ('PC RTX 5080', 'PCs with RTX 5080', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'KHUYẾN MÃI HOT' AND CategoryID = 3)),
    ('PC RTX 5070', 'PCs with RTX 5070', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'KHUYẾN MÃI HOT' AND CategoryID = 3)),
    ('PC GVN RTX 5070Ti', 'PCs with RTX 5070Ti', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'KHUYẾN MÃI HOT' AND CategoryID = 3)),
    (N'Thu cũ đổi mới VGA', 'Trade-in old VGA for new', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'KHUYẾN MÃI HOT' AND CategoryID = 3)),
    ('BTF i7 - 4070Ti Super', 'BTF i7 - 4070Ti Super PCs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'PC KHUYẾN MÃI' AND CategoryID = 3)),
    ('I5 - 4060', 'I5 - 4060 PCs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'PC KHUYẾN MÃI' AND CategoryID = 3)),
    ('I5 - 4060Ti', 'I5 - 4060Ti PCs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'PC KHUYẾN MÃI' AND CategoryID = 3)),
    ('PC RX 6600 - 12TR690', 'PCs with RX 6600 - 12TR690', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'PC KHUYẾN MÃI' AND CategoryID = 3)),
    ('PC RX 6500 - 9TR990', 'PCs with RX 6500 - 9TR990', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'PC KHUYẾN MÃI' AND CategoryID = 3)),
    (N'PC sử dụng VGA 1650', 'PCs using VGA 1650', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'PC theo cấu hình VGA' AND CategoryID = 3)),
    (N'PC sử dụng VGA 3050', 'PCs using VGA 3050', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'PC theo cấu hình VGA' AND CategoryID = 3)),
    (N'PC sử dụng VGA 3060', 'PCs using VGA 3060', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'PC theo cấu hình VGA' AND CategoryID = 3)),
    (N'PC sử dụng VGA RX 6600', 'PCs using VGA RX 6600', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'PC theo cấu hình VGA' AND CategoryID = 3)),
    (N'PC sử dụng VGA RX 6500', 'PCs using VGA RX 6500', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'PC theo cấu hình VGA' AND CategoryID = 3)),
    ('PC GVN X ASUS - PBA', 'PC GVN X ASUS - PBA', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'A.I PC - GVN' AND CategoryID = 3)),
    ('PC GVN X MSI', 'PC GVN X MSI', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'A.I PC - GVN' AND CategoryID = 3)),
    ('PC MSI - Powered by MSI', 'PC MSI - Powered by MSI', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'A.I PC - GVN' AND CategoryID = 3)),
    ('PC Core I3', 'PCs with Core I3', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'PC theo CPU Intel' AND CategoryID = 3)),
    ('PC Core I5', 'PCs with Core I5', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'PC theo CPU Intel' AND CategoryID = 3)),
    ('PC Core I7', 'PCs with Core I7', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'PC theo CPU Intel' AND CategoryID = 3)),
    ('PC Core I9', 'PCs with Core I9', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'PC theo CPU Intel' AND CategoryID = 3)),
    ('PC AMD R3', 'PCs with AMD R3', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'PC theo CPU AMD' AND CategoryID = 3)),
    ('PC AMD R5', 'PCs with AMD R5', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'PC theo CPU AMD' AND CategoryID = 3)),
    ('PC AMD R7', 'PCs with AMD R7', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'PC theo CPU AMD' AND CategoryID = 3)),
    ('PC AMD R9', 'PCs with AMD R9', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'PC theo CPU AMD' AND CategoryID = 3)),
    (N'Homework Athlon - Giá chỉ 3.990k', 'Homework Athlon - Only 3.990k', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'PC Văn phòng' AND CategoryID = 3)),
    (N'Homework R3 - Giá chỉ 5,690k', 'Homework R3 - Only 5,690k', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'PC Văn phòng' AND CategoryID = 3)),
    (N'Homework R5 - Giá chỉ 5,690k', 'Homework R5 - Only 5,690k', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'PC Văn phòng' AND CategoryID = 3)),
    (N'Homework I5 - Giá chỉ 5,690k', 'Homework I5 - Only 5,690k', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'PC Văn phòng' AND CategoryID = 3)),
    (N'Window bản quyền - Chỉ từ 2.990K', 'Licensed Windows - From 2.990K', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Phần mềm bản quyền' AND CategoryID = 3)),
    (N'Office 365 bản quyền - Chỉ từ 990K', 'Licensed Office 365 - From 990K', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Phần mềm bản quyền' AND CategoryID = 3));
GO

-- 'Main, CPU, VGA' category
INSERT INTO SubSubcategory
    (Name, Description, SubcategoryID)
VALUES
    ('RTX 5090', 'RTX 5090 GPUs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'VGA RTX 50 SERIES' AND CategoryID = 4)),
    ('RTX 5080', 'RTX 5080 GPUs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'VGA RTX 50 SERIES' AND CategoryID = 4)),
    ('RTX 5070Ti', 'RTX 5070Ti GPUs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'VGA RTX 50 SERIES' AND CategoryID = 4)),
    ('RTX 5070', 'RTX 5070 GPUs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'VGA RTX 50 SERIES' AND CategoryID = 4)),
    ('RTX 5060Ti', 'RTX 5060Ti GPUs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'VGA RTX 50 SERIES' AND CategoryID = 4)),
    ('RTX 4070 SUPER (12GB)', 'RTX 4070 SUPER (12GB) GPUs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'VGA (Trên 12 GB VRAM)' AND CategoryID = 4)),
    ('RTX 4070Ti SUPER (16GB)', 'RTX 4070Ti SUPER (16GB) GPUs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'VGA (Trên 12 GB VRAM)' AND CategoryID = 4)),
    ('RTX 4080 SUPER (16GB)', 'RTX 4080 SUPER (16GB) GPUs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'VGA (Trên 12 GB VRAM)' AND CategoryID = 4)),
    ('RTX 4090 SUPER (24GB)', 'RTX 4090 SUPER (24GB) GPUs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'VGA (Trên 12 GB VRAM)' AND CategoryID = 4)),
    ('RTX 4060Ti (8 - 16GB)', 'RTX 4060Ti (8 - 16GB) GPUs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'VGA (Dưới 12 GB VRAM)' AND CategoryID = 4)),
    ('RTX 4060 (8GB)', 'RTX 4060 (8GB) GPUs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'VGA (Dưới 12 GB VRAM)' AND CategoryID = 4)),
    ('RTX 3060 (12GB)', 'RTX 3060 (12GB) GPUs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'VGA (Dưới 12 GB VRAM)' AND CategoryID = 4)),
    ('RTX 3050 (6 - 8GB)', 'RTX 3050 (6 - 8GB) GPUs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'VGA (Dưới 12 GB VRAM)' AND CategoryID = 4)),
    ('GTX 1650 (4GB)', 'GTX 1650 (4GB) GPUs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'VGA (Dưới 12 GB VRAM)' AND CategoryID = 4)),
    ('GT 710 / GT 1030 (2-4GB)', 'GT 710 / GT 1030 (2-4GB) GPUs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'VGA (Dưới 12 GB VRAM)' AND CategoryID = 4)),
    ('NVIDIA Quadro', 'NVIDIA Quadro GPUs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'VGA - Card màn hình' AND CategoryID = 4)),
    ('AMD Radeon', 'AMD Radeon GPUs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'VGA - Card màn hình' AND CategoryID = 4)),
    ('Z890 (Mới)', 'Z890 motherboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Bo mạch chủ Intel' AND CategoryID = 4)),
    ('Z790', 'Z790 motherboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Bo mạch chủ Intel' AND CategoryID = 4)),
    ('B760', 'B760 motherboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Bo mạch chủ Intel' AND CategoryID = 4)),
    ('H610', 'H610 motherboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Bo mạch chủ Intel' AND CategoryID = 4)),
    ('X299X', 'X299X motherboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Bo mạch chủ Intel' AND CategoryID = 4)),
    (N'Xem tất cả', 'View all', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Bo mạch chủ Intel' AND CategoryID = 4)),
    (N'AMD X870 (Mới)', 'AMD X870 motherboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Bo mạch chủ AMD' AND CategoryID = 4)),
    ('AMD X670', 'AMD X670 motherboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Bo mạch chủ AMD' AND CategoryID = 4)),
    ('AMD X570', 'AMD X570 motherboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Bo mạch chủ AMD' AND CategoryID = 4)),
    (N'AMD B650 (Mới)', 'AMD B650 motherboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Bo mạch chủ AMD' AND CategoryID = 4)),
    ('AMD B550', 'AMD B550 motherboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Bo mạch chủ AMD' AND CategoryID = 4)),
    ('AMD A320', 'AMD A320 motherboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Bo mạch chủ AMD' AND CategoryID = 4)),
    ('AMD TRX40', 'AMD TRX40 motherboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Bo mạch chủ AMD' AND CategoryID = 4)),
    ('CPU Intel Core Ultra Series 2 (Mới)', 'CPU Intel Core Ultra Series 2', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'CPU - Bộ vi xử lý Intel' AND CategoryID = 4)),
    ('CPU Intel 9', 'CPU Intel 9', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'CPU - Bộ vi xử lý Intel' AND CategoryID = 4)),
    ('CPU Intel 7', 'CPU Intel 7', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'CPU - Bộ vi xử lý Intel' AND CategoryID = 4)),
    ('CPU Intel 5', 'CPU Intel 5', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'CPU - Bộ vi xử lý Intel' AND CategoryID = 4)),
    ('CPU Intel 3', 'CPU Intel 3', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'CPU - Bộ vi xử lý Intel' AND CategoryID = 4)),
    ('CPU AMD Athlon', 'CPU AMD Athlon', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'CPU - Bộ vi xử lý AMD' AND CategoryID = 4)),
    ('CPU AMD R3', 'CPU AMD R3', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'CPU - Bộ vi xử lý AMD' AND CategoryID = 4)),
    ('CPU AMD R5', 'CPU AMD R5', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'CPU - Bộ vi xử lý AMD' AND CategoryID = 4)),
    ('CPU AMD R7', 'CPU AMD R7', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'CPU - Bộ vi xử lý AMD' AND CategoryID = 4)),
    ('CPU AMD R9', 'CPU AMD R9', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'CPU - Bộ vi xử lý AMD' AND CategoryID = 4));
GO

-- 'Case, Nguồn, Tản' category
INSERT INTO SubSubcategory
    (Name, Description, SubcategoryID)
VALUES
    ('Case ASUS', 'ASUS cases', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Case - Theo hãng' AND CategoryID = 5)),
    ('Case Corsair', 'Corsair cases', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Case - Theo hãng' AND CategoryID = 5)),
    ('Case Lianli', 'Lianli cases', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Case - Theo hãng' AND CategoryID = 5)),
    ('Case NZXT', 'NZXT cases', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Case - Theo hãng' AND CategoryID = 5)),
    ('Case Inwin', 'Inwin cases', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Case - Theo hãng' AND CategoryID = 5)),
    ('Case Thermaltake', 'Thermaltake cases', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Case - Theo hãng' AND CategoryID = 5)),
    (N'Xem tất cả', 'View all', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Case - Theo hãng' AND CategoryID = 5)),
    (N'Dưới 1 triệu', 'Cases under 1 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Case - Theo giá' AND CategoryID = 5)),
    (N'Từ 1 triệu đến 2 triệu', 'Cases from 1 to 2 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Case - Theo giá' AND CategoryID = 5)),
    (N'Trên 2 triệu', 'Cases over 2 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Case - Theo giá' AND CategoryID = 5)),
    (N'Xem tất cả', 'View all', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Case - Theo giá' AND CategoryID = 5)),
    (N'Nguồn ASUS', 'ASUS power supplies', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Nguồn - Theo Hãng' AND CategoryID = 5)),
    (N'Nguồn DeepCool', 'DeepCool power supplies', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Nguồn - Theo Hãng' AND CategoryID = 5)),
    (N'Nguồn Corsair', 'Corsair power supplies', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Nguồn - Theo Hãng' AND CategoryID = 5)),
    (N'Nguồn NZXT', 'NZXT power supplies', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Nguồn - Theo Hãng' AND CategoryID = 5)),
    (N'Nguồn MSI', 'MSI power supplies', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Nguồn - Theo Hãng' AND CategoryID = 5)),
    (N'Xem tất cả', 'View all', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Nguồn - Theo Hãng' AND CategoryID = 5)),
    (N'Từ 400w - 500w', 'Power supplies from 400w to 500w', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Nguồn - Theo công suất' AND CategoryID = 5)),
    (N'Từ 500w - 600w', 'Power supplies from 500w to 600w', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Nguồn - Theo công suất' AND CategoryID = 5)),
    (N'Từ 700w - 800w', 'Power supplies from 700w to 800w', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Nguồn - Theo công suất' AND CategoryID = 5)),
    (N'Trên 1000w', 'Power supplies over 1000w', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Nguồn - Theo công suất' AND CategoryID = 5)),
    (N'Xem tất cả', 'View all', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Nguồn - Theo công suất' AND CategoryID = 5)),
    (N'Dây LED', 'LED strips', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Phụ kiện PC' AND CategoryID = 5)),
    (N'Dây rise - Dựng VGA', 'Riser cables for VGA', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Phụ kiện PC' AND CategoryID = 5)),
    (N'Giá đỡ VGA', 'VGA holders', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Phụ kiện PC' AND CategoryID = 5)),
    (N'Keo tản nhiệt', 'Thermal paste', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Phụ kiện PC' AND CategoryID = 5)),
    (N'Xem tất cả', 'View all', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Phụ kiện PC' AND CategoryID = 5)),
    (N'Tản nhiệt AIO 240mm', '240mm AIO coolers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Loại tản nhiệt' AND CategoryID = 5)),
    (N'Tản nhiệt AIO 280mm', '280mm AIO coolers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Loại tản nhiệt' AND CategoryID = 5)),
    (N'Tản nhiệt AIO 360mm', '360mm AIO coolers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Loại tản nhiệt' AND CategoryID = 5)),
    (N'Tản nhiệt AIO 420mm', '420mm AIO coolers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Loại tản nhiệt' AND CategoryID = 5)),
    (N'Tản nhiệt khí', 'Air coolers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Loại tản nhiệt' AND CategoryID = 5)),
    (N'Fan RGB', 'RGB fans', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Loại tản nhiệt' AND CategoryID = 5)),
    (N'Xem tất cả', 'View all', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Loại tản nhiệt' AND CategoryID = 5));
GO

-- 'Ổ cứng, RAM, Thẻ nhớ' category
INSERT INTO SubSubcategory
    (Name, Description, SubcategoryID)
VALUES
    ('8 GB', '8 GB RAM', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Dung lượng RAM' AND CategoryID = 6)),
    ('16 GB', '16 GB RAM', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Dung lượng RAM' AND CategoryID = 6)),
    ('32 GB', '32 GB RAM', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Dung lượng RAM' AND CategoryID = 6)),
    ('64 GB', '64 GB RAM', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Dung lượng RAM' AND CategoryID = 6)),
    (N'Xem tất cả', 'View all', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Dung lượng RAM' AND CategoryID = 6)),
    ('DDR4', 'DDR4 RAM', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Loại RAM' AND CategoryID = 6)),
    ('DDR5', 'DDR5 RAM', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Loại RAM' AND CategoryID = 6)),
    (N'Xem tất cả', 'View all', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Loại RAM' AND CategoryID = 6)),
    ('Corsair', 'Corsair RAM', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng RAM' AND CategoryID = 6)),
    ('Kingston', 'Kingston RAM', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng RAM' AND CategoryID = 6)),
    ('G.Skill', 'G.Skill RAM', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng RAM' AND CategoryID = 6)),
    ('PNY', 'PNY RAM', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng RAM' AND CategoryID = 6)),
    (N'Xem tất cả', 'View all', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng RAM' AND CategoryID = 6)),
    ('HDD 1 TB', '1 TB HDD', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Dung lượng HDD' AND CategoryID = 6)),
    ('HDD 2 TB', '2 TB HDD', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Dung lượng HDD' AND CategoryID = 6)),
    ('HDD 4 TB - 6 TB', '4 TB - 6 TB HDD', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Dung lượng HDD' AND CategoryID = 6)),
    (N'HDD trên 8 TB', 'HDD over 8 TB', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Dung lượng HDD' AND CategoryID = 6)),
    (N'Xem tất cả', 'View all', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Dung lượng HDD' AND CategoryID = 6)),
    ('WesterDigital', 'Western Digital HDD', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng HDD' AND CategoryID = 6)),
    ('Seagate', 'Seagate HDD', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng HDD' AND CategoryID = 6)),
    ('Toshiba', 'Toshiba HDD', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng HDD' AND CategoryID = 6)),
    (N'Xem tất cả', 'View all', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng HDD' AND CategoryID = 6)),
    ('120GB - 128GB', '120GB - 128GB SSD', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Dung lượng SSD' AND CategoryID = 6)),
    ('250GB - 256GB', '250GB - 256GB SSD', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Dung lượng SSD' AND CategoryID = 6)),
    ('480GB - 512GB', '480GB - 512GB SSD', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Dung lượng SSD' AND CategoryID = 6)),
    ('960GB - 1TB', '960GB - 1TB SSD', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Dung lượng SSD' AND CategoryID = 6)),
    ('2TB', '2TB SSD', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Dung lượng SSD' AND CategoryID = 6)),
    (N'Trên 2TB', 'Over 2TB SSD', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Dung lượng SSD' AND CategoryID = 6)),
    (N'Xem tất cả', 'View all', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Dung lượng SSD' AND CategoryID = 6)),
    ('Samsung', 'Samsung SSD', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng SSD' AND CategoryID = 6)),
    ('Wester Digital', 'Western Digital SSD', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng SSD' AND CategoryID = 6)),
    ('Kingston', 'Kingston SSD', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng SSD' AND CategoryID = 6)),
    ('Corsair', 'Corsair SSD', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng SSD' AND CategoryID = 6)),
    ('PNY', 'PNY SSD', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng SSD' AND CategoryID = 6)),
    (N'Xem tất cả', 'View all', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng SSD' AND CategoryID = 6)),
    ('Sandisk', 'Sandisk memory cards and USB drives', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thẻ nhớ / USB' AND CategoryID = 6)),
    ('Portable hard drives', 'Portable hard drives', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Ổ cứng di động' AND CategoryID = 6));
GO

-- 'Loa, Micro, Webcam' category
INSERT INTO SubSubcategory
    (Name, Description, SubcategoryID)
VALUES
    ('Edifier', 'Edifier speakers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu loa' AND CategoryID = 7)),
    ('Razer', 'Razer speakers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu loa' AND CategoryID = 7)),
    ('Logitech', 'Logitech speakers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu loa' AND CategoryID = 7)),
    ('SoundMax', 'SoundMax speakers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu loa' AND CategoryID = 7)),
    (N'Loa vi tính', 'Computer speakers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Kiểu Loa' AND CategoryID = 7)),
    ('Loa Bluetooth', 'Bluetooth speakers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Kiểu Loa' AND CategoryID = 7)),
    ('Loa Soundbar', 'Soundbar speakers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Kiểu Loa' AND CategoryID = 7)),
    ('Loa mini', 'Mini speakers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Kiểu Loa' AND CategoryID = 7)),
    (N'Sub phụ (Loa trầm)', 'Subwoofer speakers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Kiểu Loa' AND CategoryID = 7)),
    (N'Độ phân giải 4k', '4k resolution webcams', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Webcam' AND CategoryID = 7)),
    (N'Độ phân giải Full HD (1080p)', 'Full HD (1080p) resolution webcams', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Webcam' AND CategoryID = 7)),
    (N'Độ phân giải 720p', '720p resolution webcams', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Webcam' AND CategoryID = 7)),
    ('Micro HyperX', 'HyperX microphones', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Microphone' AND CategoryID = 7));
GO

-- 'Màn hình' category
INSERT INTO SubSubcategory
    (Name, Description, SubcategoryID)
VALUES
    ('LG', 'LG monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng sản xuất' AND CategoryID = 8)),
    ('Asus', 'Asus monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng sản xuất' AND CategoryID = 8)),
    ('ViewSonic', 'ViewSonic monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng sản xuất' AND CategoryID = 8)),
    ('Dell', 'Dell monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng sản xuất' AND CategoryID = 8)),
    ('Gigabyte', 'Gigabyte monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng sản xuất' AND CategoryID = 8)),
    ('AOC', 'AOC monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng sản xuất' AND CategoryID = 8)),
    ('Acer', 'Acer monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng sản xuất' AND CategoryID = 8)),
    ('HKC', 'HKC monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng sản xuất' AND CategoryID = 8)),
    (N'Dưới 5 triệu', 'Monitors under 5 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Giá tiền' AND CategoryID = 8)),
    (N'Từ 5 triệu đến 10 triệu', 'Monitors from 5 to 10 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Giá tiền' AND CategoryID = 8)),
    (N'Từ 10 triệu đến 20 triệu', 'Monitors from 10 to 20 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Giá tiền' AND CategoryID = 8)),
    (N'Từ 20 triệu đến 30 triệu', 'Monitors from 20 to 30 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Giá tiền' AND CategoryID = 8)),
    (N'Trên 30 triệu', 'Monitors over 30 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Giá tiền' AND CategoryID = 8)),
    (N'Màn hình Full HD', 'Full HD monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Độ Phân giải' AND CategoryID = 8)),
    (N'Màn hình 2K 1440p', '2K 1440p monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Độ Phân giải' AND CategoryID = 8)),
    (N'Màn hình 4K UHD', '4K UHD monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Độ Phân giải' AND CategoryID = 8)),
    (N'Màn hình 6K', '6K monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Độ Phân giải' AND CategoryID = 8)),
    ('60Hz', '60Hz monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Tần số quét' AND CategoryID = 8)),
    ('75Hz', '75Hz monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Tần số quét' AND CategoryID = 8)),
    ('100Hz', '100Hz monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Tần số quét' AND CategoryID = 8)),
    ('144Hz', '144Hz monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Tần số quét' AND CategoryID = 8)),
    ('240Hz', '240Hz monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Tần số quét' AND CategoryID = 8)),
    ('24" Curved', '24" curved monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Màn hình cong' AND CategoryID = 8)),
    ('27" Curved', '27" curved monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Màn hình cong' AND CategoryID = 8)),
    ('32" Curved', '32" curved monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Màn hình cong' AND CategoryID = 8)),
    (N'Trên 32" Curved', 'Over 32" curved monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Màn hình cong' AND CategoryID = 8)),
    (N'Màn hình 22"', '22" monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Kích thước' AND CategoryID = 8)),
    (N'Màn hình 24"', '24" monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Kích thước' AND CategoryID = 8)),
    (N'Màn hình 27"', '27" monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Kích thước' AND CategoryID = 8)),
    (N'Màn hình 29"', '29" monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Kích thước' AND CategoryID = 8)),
    (N'Màn hình 32"', '32" monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Kích thước' AND CategoryID = 8)),
    (N'Màn hình Trên 32"', 'Over 32" monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Kích thước' AND CategoryID = 8)),
    (N'Hỗ trợ giá treo (VESA)', 'Monitors with VESA mount support', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Kích thước' AND CategoryID = 8)),
    (N'Màn hình đồ họa 24"', '24" graphic design monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Màn hình đồ họa' AND CategoryID = 8)),
    (N'Màn hình đồ họa 27"', '27" graphic design monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Màn hình đồ họa' AND CategoryID = 8)),
    (N'Màn hình đồ họa 32"', '32" graphic design monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Màn hình đồ họa' AND CategoryID = 8)),
    (N'Giá treo màn hình', 'Monitor mounts', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Phụ kiện màn hình' AND CategoryID = 8)),
    (N'Phụ kiện dây HDMI,DP,LAN', 'HDMI, DP, LAN cables', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Phụ kiện màn hình' AND CategoryID = 8)),
    ('Full HD 1080p', 'Portable monitors with Full HD 1080p', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Màn hình di động' AND CategoryID = 8)),
    ('2K 1440p', 'Portable monitors with 2K 1440p', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Màn hình di động' AND CategoryID = 8)),
    (N'Có cảm ứng', 'Touchscreen portable monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Màn hình di động' AND CategoryID = 8)),
    (N'Màn hình Oled', 'OLED monitors', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Màn hình Oled' AND CategoryID = 8));
GO

-- 'Bàn phím' category
INSERT INTO SubSubcategory
    (Name, Description, SubcategoryID)
VALUES
    ('AKKO', 'AKKO keyboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 9)),
    ('AULA', 'AULA keyboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 9)),
    ('Dare-U', 'Dare-U keyboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 9)),
    ('Durgod', 'Durgod keyboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 9)),
    ('Leobog', 'Leobog keyboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 9)),
    ('FL-Esports', 'FL-Esports keyboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 9)),
    ('Corsair', 'Corsair keyboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 9)),
    ('E-Dra', 'E-Dra keyboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 9)),
    ('Cidoo', 'Cidoo keyboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 9)),
    ('Machenike', 'Machenike keyboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu' AND CategoryID = 9)),
    (N'Dưới 1 triệu', 'Keyboards under 1 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Giá tiền' AND CategoryID = 9)),
    (N'1 triệu - 2 triệu', 'Keyboards from 1 to 2 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Giá tiền' AND CategoryID = 9)),
    (N'2 triệu - 3 triệu', 'Keyboards from 2 to 3 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Giá tiền' AND CategoryID = 9)),
    (N'3 triệu - 4 triệu', 'Keyboards from 3 to 4 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Giá tiền' AND CategoryID = 9)),
    (N'Trên 4 triệu', 'Keyboards over 4 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Giá tiền' AND CategoryID = 9)),
    ('Bluetooth', 'Bluetooth keyboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Kết nối' AND CategoryID = 9)),
    ('Wireless', 'Wireless keyboards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Kết nối' AND CategoryID = 9)),
    ('Keycaps', 'Keycaps', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Phụ kiện bàn phím cơ' AND CategoryID = 9)),
    ('Dwarf Factory', 'Dwarf Factory keycaps', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Phụ kiện bàn phím cơ' AND CategoryID = 9)),
    (N'Kê tay', 'Wrist rests', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Phụ kiện bàn phím cơ' AND CategoryID = 9));
GO

-- 'Chuột + Lót chuột' category
INSERT INTO SubSubcategory
    (Name, Description, SubcategoryID)
VALUES
    ('Logitech', 'Logitech mice', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu chuột' AND CategoryID = 10)),
    ('Razer', 'Razer mice', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu chuột' AND CategoryID = 10)),
    ('Corsair', 'Corsair mice', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu chuột' AND CategoryID = 10)),
    ('Pulsar', 'Pulsar mice', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu chuột' AND CategoryID = 10)),
    ('Microsoft', 'Microsoft mice', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu chuột' AND CategoryID = 10)),
    ('Dare U', 'Dare U mice', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu chuột' AND CategoryID = 10)),
    (N'Dưới 500 nghìn', 'Mice under 500 thousand', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Chuột theo giá tiền' AND CategoryID = 10)),
    (N'Từ 500 nghìn - 1 triệu', 'Mice from 500 thousand to 1 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Chuột theo giá tiền' AND CategoryID = 10)),
    (N'Từ 1 triệu - 2 triệu', 'Mice from 1 to 2 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Chuột theo giá tiền' AND CategoryID = 10)),
    (N'Trên 2 triệu - 3 triệu', 'Mice from 2 to 3 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Chuột theo giá tiền' AND CategoryID = 10)),
    (N'Trên 3 triệu', 'Mice over 3 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Chuột theo giá tiền' AND CategoryID = 10)),
    (N'Chuột chơi game', 'Gaming mice', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Loại Chuột' AND CategoryID = 10)),
    (N'Chuột văn phòng', 'Office mice', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Loại Chuột' AND CategoryID = 10)),
    ('Logitech Gaming', 'Logitech gaming mice', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Logitech' AND CategoryID = 10)),
    (N'Logitech Văn phòng', 'Logitech office mice', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Logitech' AND CategoryID = 10)),
    ('GEARVN', 'GEARVN mousepads', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu lót chuột' AND CategoryID = 10)),
    ('ASUS', 'ASUS mousepads', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu lót chuột' AND CategoryID = 10)),
    ('Steelseries', 'Steelseries mousepads', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu lót chuột' AND CategoryID = 10)),
    ('Dare-U', 'Dare-U mousepads', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu lót chuột' AND CategoryID = 10)),
    ('Razer', 'Razer mousepads', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu lót chuột' AND CategoryID = 10)),
    (N'Mềm', 'Soft mousepads', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Các loại lót chuột' AND CategoryID = 10)),
    (N'Cứng', 'Hard mousepads', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Các loại lót chuột' AND CategoryID = 10)),
    (N'Dày', 'Thick mousepads', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Các loại lót chuột' AND CategoryID = 10)),
    (N'Mỏng', 'Thin mousepads', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Các loại lót chuột' AND CategoryID = 10)),
    (N'Viền có led', 'Mousepads with LED edges', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Các loại lót chuột' AND CategoryID = 10)),
    (N'Nhỏ', 'Small mousepads', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Lót chuột theo size' AND CategoryID = 10)),
    (N'Vừa', 'Medium mousepads', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Lót chuột theo size' AND CategoryID = 10)),
    (N'Lớn', 'Large mousepads', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Lót chuột theo size' AND CategoryID = 10));
GO

-- 'Tai Nghe' category
INSERT INTO SubSubcategory
    (Name, Description, SubcategoryID)
VALUES
    ('ASUS', 'ASUS headphones', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu tai nghe' AND CategoryID = 11)),
    ('HyperX', 'HyperX headphones', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu tai nghe' AND CategoryID = 11)),
    ('Corsair', 'Corsair headphones', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu tai nghe' AND CategoryID = 11)),
    ('Razer', 'Razer headphones', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu tai nghe' AND CategoryID = 11)),
    (N'Tai nghe dưới 1 triệu', 'Headphones under 1 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Tai nghe theo giá' AND CategoryID = 11)),
    (N'Tai nghe 1 triệu đến 2 triệu', 'Headphones from 1 to 2 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Tai nghe theo giá' AND CategoryID = 11)),
    (N'Tai nghe 2 đến 3 triệu', 'Headphones from 2 to 3 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Tai nghe theo giá' AND CategoryID = 11)),
    (N'Tai nghe 3 đến 4 triệu', 'Headphones from 3 to 4 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Tai nghe theo giá' AND CategoryID = 11)),
    (N'Tai nghe trên 4 triệu', 'Headphones over 4 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Tai nghe theo giá' AND CategoryID = 11)),
    (N'Tai nghe Wireless', 'Wireless headphones', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Kiểu kết nối' AND CategoryID = 11)),
    (N'Tai nghe Bluetooth', 'Bluetooth headphones', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Kiểu kết nối' AND CategoryID = 11)),
    ('Tai nghe Over-ear', 'Over-ear headphones', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Kiểu tai nghe' AND CategoryID = 11)),
    ('Tai nghe Gaming In-ear', 'Gaming in-ear headphones', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Kiểu tai nghe' AND CategoryID = 11));
GO

-- 'Ghế - Bàn' category
INSERT INTO SubSubcategory
    (Name, Description, SubcategoryID)
VALUES
    ('Corsair', 'Corsair gaming chairs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu ghế Gaming' AND CategoryID = 12)),
    ('Warrior', 'Warrior gaming chairs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu ghế Gaming' AND CategoryID = 12)),
    ('E-DRA', 'E-DRA gaming chairs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu ghế Gaming' AND CategoryID = 12)),
    ('DXRacer', 'DXRacer gaming chairs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu ghế Gaming' AND CategoryID = 12)),
    ('Cougar', 'Cougar gaming chairs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu ghế Gaming' AND CategoryID = 12)),
    ('AKRaing', 'AKRaing gaming chairs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu ghế Gaming' AND CategoryID = 12)),
    ('Warrior', 'Warrior ergonomic chairs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu ghế CTH' AND CategoryID = 12)),
    ('Sihoo', 'Sihoo ergonomic chairs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu ghế CTH' AND CategoryID = 12)),
    ('E-Dra', 'E-Dra ergonomic chairs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Thương hiệu ghế CTH' AND CategoryID = 12)),
    (N'Ghế Công thái học', 'Ergonomic chairs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Kiểu ghế' AND CategoryID = 12)),
    (N'Ghế Gaming', 'Gaming chairs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Kiểu ghế' AND CategoryID = 12)),
    (N'Bàn Gaming DXRacer', 'DXRacer gaming desks', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Bàn Gaming' AND CategoryID = 12)),
    (N'Bàn Gaming E-Dra', 'E-Dra gaming desks', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Bàn Gaming' AND CategoryID = 12)),
    (N'Bàn Gaming Warrior', 'Warrior gaming desks', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Bàn Gaming' AND CategoryID = 12)),
    (N'Bàn CTH Warrior', 'Warrior ergonomic desks', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Bàn công thái học' AND CategoryID = 12)),
    (N'Phụ kiện bàn ghế', 'Desk and chair accessories', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Bàn công thái học' AND CategoryID = 12)),
    (N'Dưới 5 triệu', 'Chairs and desks under 5 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Giá tiền' AND CategoryID = 12)),
    (N'Từ 5 đến 10 triệu', 'Chairs and desks from 5 to 10 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Giá tiền' AND CategoryID = 12)),
    (N'Trên 10 triệu', 'Chairs and desks over 10 million', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Giá tiền' AND CategoryID = 12));
GO

-- 'Phần mềm, mạng' category
INSERT INTO SubSubcategory
    (Name, Description, SubcategoryID)
VALUES
    ('Asus', 'Asus networking devices', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng sản xuất' AND CategoryID = 13)),
    ('LinkSys', 'LinkSys networking devices', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng sản xuất' AND CategoryID = 13)),
    ('TP-LINK', 'TP-LINK networking devices', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng sản xuất' AND CategoryID = 13)),
    ('Mercusys', 'Mercusys networking devices', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hãng sản xuất' AND CategoryID = 13)),
    ('Gaming', 'Gaming routers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Router Wi-Fi' AND CategoryID = 13)),
    (N'Phổ thông', 'Standard routers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Router Wi-Fi' AND CategoryID = 13)),
    (N'Xuyên tường', 'Wall-penetrating routers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Router Wi-Fi' AND CategoryID = 13)),
    ('Router Mesh Pack', 'Mesh router packs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Router Wi-Fi' AND CategoryID = 13)),
    ('Router WiFi 5', 'WiFi 5 routers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Router Wi-Fi' AND CategoryID = 13)),
    ('Router WiFi 6', 'WiFi 6 routers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Router Wi-Fi' AND CategoryID = 13)),
    ('Usb WiFi', 'USB WiFi adapters', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'USB Thu sóng - Card mạng' AND CategoryID = 13)),
    ('Card WiFi', 'WiFi cards', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'USB Thu sóng - Card mạng' AND CategoryID = 13)),
    (N'Dây cáp mạng', 'Network cables', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'USB Thu sóng - Card mạng' AND CategoryID = 13)),
    ('Microsoft Office 365', 'Microsoft Office 365', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Microsoft Office' AND CategoryID = 13)),
    ('Office Home 2024', 'Office Home 2024', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Microsoft Office' AND CategoryID = 13)),
    ('Windows 11 Home', 'Windows 11 Home', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Microsoft Windows' AND CategoryID = 13)),
    ('Windows 11 Pro', 'Windows 11 Pro', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Microsoft Windows' AND CategoryID = 13));
GO

-- 'Handheld, Console' category
INSERT INTO SubSubcategory
    (Name, Description, SubcategoryID)
VALUES
    ('Rog Ally', 'Rog Ally handheld PCs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Handheld PC' AND CategoryID = 14)),
    ('MSI Claw', 'MSI Claw handheld PCs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Handheld PC' AND CategoryID = 14)),
    ('Legion Go', 'Legion Go handheld PCs', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Handheld PC' AND CategoryID = 14)),
    (N'Tay cầm Playstation', 'Playstation controllers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Tay cầm' AND CategoryID = 14)),
    (N'Tay cầm Rapoo', 'Rapoo controllers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Tay cầm' AND CategoryID = 14)),
    (N'Tay cầm DareU', 'DareU controllers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Tay cầm' AND CategoryID = 14)),
    (N'Xem tất cả', 'View all', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Tay cầm' AND CategoryID = 14)),
    (N'Vô lăng lái xe, máy bay', 'Steering wheels and flight sticks', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Vô lăng lái xe, máy bay' AND CategoryID = 14)),
    (N'Sony PS5 (Máy) chính hãng', 'Official Sony PS5 consoles', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Sony Playstation' AND CategoryID = 14)),
    (N'Tay cầm chính hãng', 'Official controllers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Sony Playstation' AND CategoryID = 14));
GO

-- 'Phụ kiện (Hub, sạc, cáp..)' category
INSERT INTO SubSubcategory
    (Name, Description, SubcategoryID)
VALUES
    (N'Hub chuyển đổi', 'Hub adapters', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hub, sạc, cáp' AND CategoryID = 15)),
    (N'Dây cáp', 'Cables', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hub, sạc, cáp' AND CategoryID = 15)),
    (N'Củ sạc', 'Chargers', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Hub, sạc, cáp' AND CategoryID = 15)),
    (N'Quạt cầm tay, Quạt mini', 'Handheld and mini fans', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Quạt cầm tay, Quạt mini' AND CategoryID = 15));
GO

-- 'Dịch vụ và thông tin khác' category
INSERT INTO SubSubcategory
    (Name, Description, SubcategoryID)
VALUES
    (N'Dịch vụ kỹ thuật tại nhà', 'Home technical services', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Dịch vụ' AND CategoryID = 16)),
    (N'Dịch vụ sửa chữa', 'Repair services', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Dịch vụ' AND CategoryID = 16)),
    (N'Chính sách & bảng giá thu VGA qua sử dụng', 'Policy and price list for used VGA', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Chính sách' AND CategoryID = 16)),
    (N'Chính sách bảo hành', 'Warranty policy', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Chính sách' AND CategoryID = 16)),
    (N'Chính sách giao hàng', 'Delivery policy', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Chính sách' AND CategoryID = 16)),
    (N'Chính sách đổi trả', 'Return policy', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = N'Chính sách' AND CategoryID = 16)),
    (N'Build PC', 'PC building services', (SELECT SubcategoryID
        FROM Subcategory
        WHERE Name = 'Build PC' AND CategoryID = 16));
GO



-- Thêm thuộc tính (Attribute) nếu chưa có
INSERT INTO Attribute
    (AttributeName, AttributeType)
VALUES
    ('CPU', 'Text'),
    ('RAM', 'Text'),
    ('SSD', 'Text'),
    ('VGA', 'Text'),
    ('Screen', 'Text'),
    ('Battery', 'Text'),
    ('Weight', 'Text'),
    ('Dimensions', 'Text'),
    ('Ports', 'Text'),
    ('Keyboard', 'Text'),
    ('Audio', 'Text'),
    ('Webcam', 'Text'),
    ('Wireless', 'Text'),
    ('Operating System', 'Text'),
    ('Cooling System', 'Text'),
    ('Additional Features', 'Text'),
    ('Warranty', 'Text'),
    ('Color', 'Text');

-- Thêm giá trị thuộc tính (AttributeValue) nếu chưa có
INSERT INTO AttributeValue
    (AttributeID, ValueName)
    SELECT AttributeID, 'Intel Core i7-12700H'
    FROM Attribute
    WHERE AttributeName = 'CPU'
UNION ALL
    SELECT AttributeID, '16GB DDR5 4800MHz'
    FROM Attribute
    WHERE AttributeName = 'RAM'
UNION ALL
    SELECT AttributeID, '512GB NVMe PCIe 4.0'
    FROM Attribute
    WHERE AttributeName = 'SSD'
UNION ALL
    SELECT AttributeID, 'NVIDIA GeForce RTX 3060 6GB GDDR6'
    FROM Attribute
    WHERE AttributeName = 'VGA'
UNION ALL
    SELECT AttributeID, '15.6\" Full HD (1920x1080) 144Hz IPS'
    FROM Attribute
    WHERE AttributeName = 'Screen'
UNION ALL
    SELECT AttributeID, '90Wh, up to 8 hours'
    FROM Attribute
    WHERE AttributeName = 'Battery'
UNION ALL
    SELECT AttributeID, '2.3 kg'
    FROM Attribute
    WHERE AttributeName = 'Weight'
UNION ALL
    SELECT AttributeID, '35.4 x 25.9 x 2.26 cm'
    FROM Attribute
    WHERE AttributeName = 'Dimensions'
UNION ALL
    SELECT AttributeID, '1x USB-C (DisplayPort, Power Delivery), 2x USB-A 3.2, 1x USB 2.0, 1x HDMI 2.0b, 1x RJ45, 1x SD card, 1x 3.5mm audio'
    FROM Attribute
    WHERE AttributeName = 'Ports'
UNION ALL
    SELECT AttributeID, 'RGB backlit chiclet, N-key rollover, 1.7mm key travel'
    FROM Attribute
    WHERE AttributeName = 'Keyboard'
UNION ALL
    SELECT AttributeID, '2 stereo speakers, Dolby Atmos, AI noise-canceling'
    FROM Attribute
    WHERE AttributeName = 'Audio'
UNION ALL
    SELECT AttributeID, '720p HD'
    FROM Attribute
    WHERE AttributeName = 'Webcam'
UNION ALL
    SELECT AttributeID, 'Wi-Fi 6 (802.11ax), Bluetooth 5.2'
    FROM Attribute
    WHERE AttributeName = 'Wireless'
UNION ALL
    SELECT AttributeID, 'Windows 11 Home'
    FROM Attribute
    WHERE AttributeName = 'Operating System'
UNION ALL
    SELECT AttributeID, 'Dual fans, 4 heat pipes, self-cleaning dust technology'
    FROM Attribute
    WHERE AttributeName = 'Cooling System'
UNION ALL
    SELECT AttributeID, 'ASUS Armoury Crate, GameVisual, TPM 2.0'
    FROM Attribute
    WHERE AttributeName = 'Additional Features'
UNION ALL
    SELECT AttributeID, '2 years'
    FROM Attribute
    WHERE AttributeName = 'Warranty'
UNION ALL
    SELECT AttributeID, 'Eclipse Gray'
    FROM Attribute
    WHERE AttributeName = 'Color';

-- Thêm sản phẩm vào bảng Product
-- Giả định SubSubcategoryID = 1 (cần thay bằng ID thực tế từ cơ sở dữ liệu)
INSERT INTO Product
    (Name, Description, Price, Stock, SubSubcategoryID, Brand)
VALUES
    ('Laptop Gaming ASUS ROG', 'Laptop gaming cao cấp với cấu hình mạnh mẽ, màn hình 144Hz, và hệ thống tản nhiệt tiên tiến.', 29990000.00, 50, 1, 'ASUS');

DECLARE @ProductID INT = SCOPE_IDENTITY();

-- Thêm hình ảnh sản phẩm
INSERT INTO ProductImage
    (ProductID, ImageURL, IsPrimary, DisplayOrder)
VALUES
    (@ProductID, '/placeholder.svg?height=150&width=150', 1, 1),
    (@ProductID, '/placeholder.svg?height=100&width=100', 0, 2);

-- Liên kết thuộc tính với sản phẩm
INSERT INTO ProductAttributeValue
    (ProductID, ValueID)
SELECT @ProductID, ValueID
FROM AttributeValue
WHERE ValueName IN (
    'Intel Core i7-12700H',
    '16GB DDR5 4800MHz',
    '512GB NVMe PCIe 4.0',
    'NVIDIA GeForce RTX 3060 6GB GDDR6',
    '15.6\" Full HD (1920x1080) 144Hz IPS',
    '90Wh, up to 8 hours',
    '2.3 kg',
    '35.4 x 25.9 x 2.26 cm',
    '1x USB-C (DisplayPort, Power Delivery), 2x USB-A 3.2, 1x USB 2.0, 1x HDMI 2.0b, 1x RJ45, 1x SD card, 1x 3.5mm audio',
    'RGB backlit chiclet, N-key rollover, 1.7mm key travel',
    '2 stereo speakers, Dolby Atmos, AI noise-canceling',
    '720p HD',
    'Wi-Fi 6 (802.11ax), Bluetooth 5.2',
    'Windows 11 Home',
    'Dual fans, 4 heat pipes, self-cleaning dust technology',
    'ASUS Armoury Crate, GameVisual, TPM 2.0',
    '2 years',
    'Eclipse Gray'
);


CREATE TABLE CategoryAttributes
(
    CategoryAttributeID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryID INT NOT NULL,
    AttributeName VARCHAR(100) NOT NULL,
    FOREIGN KEY (CategoryID) REFERENCES Category(CategoryID)
);

-- Ví dụ dữ liệu
INSERT INTO CategoryAttributes
    (CategoryID, AttributeName)
VALUES
    (1, 'CPU'),
    -- Laptop
    (1, 'RAM'),
    (1, 'SSD'),
    (2, 'LED RGB'),
    -- Chuột
    (2, 'Kết nối');

-- Thêm ràng buộc
ALTER TABLE Product
ADD CONSTRAINT UQ_Product_Name UNIQUE (Name);

ALTER TABLE Product
ADD CONSTRAINT CHK_Product_Price CHECK (Price > 0);

ALTER TABLE Product
ADD CONSTRAINT CHK_Product_Stock CHECK (Stock >= 0);

-- Thêm Index
CREATE INDEX IX_Product_Name ON Product(Name);
CREATE INDEX IX_Product_Price ON Product(Price);
CREATE INDEX IX_Product_Brand ON Product(Brand);

-- Thêm dữ liệu khách hàng mẫu
INSERT INTO Customer
    (Name, Username, Email, Password, Phone, Address)
VALUES
    (N'Nguyễn Văn A', 'nguyenvana', 'nguyenvana@email.com', 'hashed_password_1', '0123456789', N'Hà Nội'),
    (N'Trần Thị B', 'tranthib', 'tranthib@email.com', 'hashed_password_2', '0987654321', N'Hồ Chí Minh');

-- Thêm giỏ hàng mẫu
INSERT INTO Cart
    (UserID, TotalPrice)
VALUES
    (1, 0),
    (2, 0);

-- Thêm sản phẩm mẫu
INSERT INTO Product
    (Name, Description, Price, Stock, SubSubcategoryID, Brand)
VALUES
    (N'Laptop Gaming MSI', N'Laptop gaming MSI cấu hình cao', 25990000, 30, 2, 'MSI'),
    (N'Laptop Dell XPS', N'Laptop cao cấp Dell XPS', 32990000, 20, 3, 'Dell');

-- Thêm vào giỏ hàng
INSERT INTO CartItem
    (CartID, ProductID, Quantity, Subtotal)
VALUES
    (1, 1, 1, 29990000),
    (2, 2, 1, 25990000);

-- Thêm đơn hàng mẫu
INSERT INTO Orders
    (UserID, TotalPrice, FinalPrice, Status)
VALUES
    (1, 29990000, 29990000, 'Delivered'),
    (2, 25990000, 25990000, 'Processing');

-- Thêm chi tiết đơn hàng
INSERT INTO OrderItem
    (OrderID, ProductID, Quantity, Subtotal, FinalSubtotal)
VALUES
    (1, 1, 1, 29990000, 29990000),
    (2, 2, 1, 25990000, 25990000);

-- Thêm thanh toán
INSERT INTO Payment
    (OrderID, Amount, PaymentMethod, PaymentStatus)
VALUES
    (1, 29990000, 'BankTransfer', 'Completed'),
    (2, 25990000, 'COD', 'Pending');

-- Thêm vận chuyển
INSERT INTO Shipping
    (OrderID, Address, Phone, ShippingMethod, ShippingCost, Status)
VALUES
    (1, N'Hà Nội', '0123456789', 'Express', 50000, 'Delivered'),
    (2, N'Hồ Chí Minh', '0987654321', 'Standard', 30000, 'InTransit');

-- Thêm đánh giá
INSERT INTO Review
    (UserID, ProductID, Rating, Comment, CreatedAt)
VALUES
    (1, 1, 5, N'Sản phẩm tốt, giao hàng nhanh', GETDATE()),
    (2, 2, 4, N'Sản phẩm đẹp nhưng giá hơi cao', GETDATE());


-- Insert Subcategories
INSERT INTO Subcategory
    (Name, Description, CategoryID)
VALUES
    (N'ASUS ROG', N'Laptop gaming ASUS ROG', 1),
    (N'MSI Gaming', N'Laptop gaming MSI', 1),
    (N'Lenovo ThinkPad', N'Laptop văn phòng Lenovo', 2),
    (N'Dell Latitude', N'Laptop văn phòng Dell', 2),
    (N'PC RTX 40 Series', N'PC gaming với card RTX 40', 3),
    (N'Chuột Gaming', N'Chuột chuyên game', 4),
    (N'Màn Hình Gaming', N'Màn hình gaming độ phân giải cao', 5),
    (N'Bàn Phím Cơ', N'Bàn phím cơ gaming', 6);

-- Insert SubSubcategories
INSERT INTO SubSubcategory
    (Name, Description, SubcategoryID)
VALUES
    (N'ROG Strix', N'Dòng laptop gaming ROG Strix', 1),
    (N'ROG Zephyrus', N'Dòng laptop gaming ROG Zephyrus', 1),
    (N'MSI Stealth', N'Dòng laptop gaming MSI Stealth', 2),
    (N'ThinkPad T Series', N'Dòng laptop văn phòng ThinkPad T', 3),
    (N'Latitude 5000', N'Dòng laptop văn phòng Latitude 5000', 4),
    (N'PC RTX 4090', N'PC gaming RTX 4090', 5),
    (N'Chuột Không Dây', N'Chuột gaming không dây', 6),
    (N'Màn Hình 4K', N'Màn hình gaming 4K', 7),
    (N'Bàn Phím Cơ RGB', N'Bàn phím cơ gaming RGB', 8);

-- Insert Products
INSERT INTO Product
    (Name, Description, Price, Stock, SubSubcategoryID, Brand)
VALUES
    -- Laptop Gaming
    (N'ASUS ROG Strix G16 G614JV',
        N'CPU: Intel Core i7-13650HX\nRAM: 16GB DDR5\nGPU: NVIDIA RTX 4060 8GB\nMàn hình: 16" QHD 240Hz\nỔ cứng: 1TB SSD',
        32990000, 10, 1, N'ASUS'),

    (N'MSI Stealth 16 Studio',
        N'CPU: Intel Core i9-13900H\nRAM: 32GB DDR5\nGPU: NVIDIA RTX 4070 8GB\nMàn hình: 16" UHD+ 100% DCI-P3\nỔ cứng: 2TB SSD',
        49990000, 5, 3, N'MSI'),

    -- Laptop Văn Phòng
    (N'Lenovo ThinkPad T14 Gen 4',
        N'CPU: Intel Core i7-1365U\nRAM: 16GB DDR5\nGPU: Intel Iris Xe\nMàn hình: 14" FHD\nỔ cứng: 512GB SSD',
        24990000, 15, 4, N'Lenovo'),

    (N'Dell Latitude 5430',
        N'CPU: Intel Core i5-1235U\nRAM: 8GB DDR4\nGPU: Intel Iris Xe\nMàn hình: 14" FHD\nỔ cứng: 256GB SSD',
        18990000, 20, 5, N'Dell'),

    -- PC Gaming
    (N'PC Gaming RTX 4090',
        N'CPU: Intel Core i9-13900K\nRAM: 32GB DDR5\nGPU: NVIDIA RTX 4090 24GB\nỔ cứng: 2TB NVMe SSD',
        69990000, 3, 6, N'Custom'),

    -- Chuột
    (N'Logitech G Pro X Superlight',
        N'Chuột không dây gaming\nCảm biến: HERO 25K\nTrọng lượng: 63g\nThời lượng pin: 70 giờ',
        2999000, 30, 7, N'Logitech'),

    -- Màn Hình
    (N'ASUS ROG Swift PG279QM',
        N'Màn hình gaming 27" QHD\nTần số quét: 240Hz\nPanel: IPS\nHDR: 600',
        15990000, 8, 8, N'ASUS'),

    -- Bàn Phím
    (N'Keychron K8 Pro',
        N'Bàn phím cơ 87 phím\nSwitch: Gateron Pro\nKết nối: Bluetooth/Type-C\nRGB Backlit',
        2499000, 25, 9, N'Keychron');

-- Insert Product Images
INSERT INTO ProductImage
    (ProductID, ImageURL, IsPrimary, DisplayOrder, CreatedAt)
VALUES
    (1, '/images/products/laptop/asus-rog-strix-g16.jpg', 1, 1, GETDATE()),
    (2, '/images/products/laptop/msi-stealth-16.jpg', 1, 1, GETDATE()),
    (3, '/images/products/laptop/thinkpad-t14.jpg', 1, 1, GETDATE()),
    (4, '/images/products/laptop/latitude-5430.jpg', 1, 1, GETDATE()),
    (5, '/images/products/pc/rtx-4090-pc.jpg', 1, 1, GETDATE()),
    (6, '/images/products/mouse/g-pro-x-superlight.jpg', 1, 1, GETDATE()),
    (7, '/images/products/monitor/rog-swift-pg279qm.jpg', 1, 1, GETDATE()),
    (8, '/images/products/keyboard/keychron-k8-pro.jpg', 1, 1, GETDATE());

-- Insert Attributes
INSERT INTO Attribute
    (AttributeName, AttributeType)
VALUES
    (N'CPU', N'Text'),
    (N'RAM', N'Text'),
    (N'GPU', N'Text'),
    (N'Màn hình', N'Text'),
    (N'Ổ cứng', N'Text'),
    (N'Cảm biến', N'Text'),
    (N'Tần số quét', N'Text'),
    (N'Switch', N'Text');

-- Insert Attribute Values
INSERT INTO AttributeValue
    (AttributeID, ValueName)
VALUES
    (1, N'Intel Core i7-13650HX'),
    (1, N'Intel Core i9-13900H'),
    (1, N'Intel Core i7-1365U'),
    (1, N'Intel Core i5-1235U'),
    (1, N'Intel Core i9-13900K'),
    (2, N'16GB DDR5'),
    (2, N'32GB DDR5'),
    (2, N'8GB DDR4'),
    (3, N'NVIDIA RTX 4060 8GB'),
    (3, N'NVIDIA RTX 4070 8GB'),
    (3, N'Intel Iris Xe'),
    (3, N'NVIDIA RTX 4090 24GB'),
    (4, N'16" QHD 240Hz'),
    (4, N'16" UHD+ 100% DCI-P3'),
    (4, N'14" FHD'),
    (5, N'1TB SSD'),
    (5, N'2TB SSD'),
    (5, N'512GB SSD'),
    (5, N'256GB SSD'),
    (5, N'2TB NVMe SSD'),
    (6, N'HERO 25K'),
    (7, N'240Hz'),
    (8, N'Gateron Pro');

-- Insert Product Attribute Values
INSERT INTO ProductAttributeValue
    (ProductID, ValueID)
VALUES
    (2, 2),
    (2, 7),
    (2, 10),
    (2, 14),
    (2, 17),
    (3, 3),
    (3, 6),
    (3, 11),
    (3, 15),
    (3, 18),
    (4, 4),
    (4, 8),
    (4, 11),
    (4, 15),
    (4, 19),
    (5, 5),
    (5, 7),
    (5, 12),
    (5, 20),
    (6, 21),
    (7, 22),
    (8, 23);

-- Insert Reviews
INSERT INTO Review
    (UserID, ProductID, Rating, Comment, CreatedAt)
VALUES
    (1, 1, 5, N'Sản phẩm tuyệt vời, chơi game mượt mà', GETDATE());
--(2, 1, 4, N'Pin hơi nhanh hết', GETDATE()),
--(3, 2, 5, N'Hiệu năng cao, thiết kế đẹp', GETDATE()),
--(4, 3, 4, N'Phù hợp cho công việc văn phòng', GETDATE()),
--(5, 4, 5, N'Bền bỉ, pin trâu', GETDATE()),
--(6, 5, 5, N'PC gaming mạnh nhất hiện tại', GETDATE()),
---(7, 6, 5, N'Chuột nhẹ, cảm giác tốt', GETDATE()),
---(8, 7, 4, N'Màn hình đẹp, giá hơi cao', GETDATE()),
---(9, 8, 5, N'Bàn phím cơ tốt nhất trong tầm giá', GETDATE());

INSERT INTO Staff
    (Name, Username, Email, Password, Role, Salary)
VALUES
    ('Admin', 'admin', 'admin@gmail.com', '123123', 'SuperAdmin', 10000000);
