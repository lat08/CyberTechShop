document.addEventListener("DOMContentLoaded", () => {
    // Tab functionality
    const tabButtons = document.querySelectorAll(".tab-btn")
    const tabContents = document.querySelectorAll(".tab-content")

    tabButtons.forEach((button) => {
        button.addEventListener("click", () => {
            tabButtons.forEach((btn) => btn.classList.remove("active"))
            tabContents.forEach((content) => content.classList.remove("active"))
            button.classList.add("active")
            const tabId = button.getAttribute("data-tab")
            document.getElementById(tabId).classList.add("active")
        })
    })

    // Product gallery thumbnails
    const mainImage = document.getElementById("mainImage")
    const thumbnails = document.querySelectorAll(".thumbnail")

    thumbnails.forEach((thumbnail) => {
        thumbnail.addEventListener("click", () => {
            thumbnails.forEach((t) => t.classList.remove("active"))
            thumbnail.classList.add("active")
            const imageUrl = thumbnail.getAttribute("data-image")
            mainImage.src = imageUrl
        })
    })

    // Quantity controls
    const quantityInput = document.getElementById("quantity")
    const minusBtn = document.querySelector(".quantity-btn.minus")
    const plusBtn = document.querySelector(".quantity-btn.plus")

    if (quantityInput && minusBtn && plusBtn) {
        minusBtn.addEventListener("click", () => {
            const currentValue = parseInt(quantityInput.value)
            if (currentValue > parseInt(quantityInput.min)) {
                quantityInput.value = currentValue - 1
            }
        })

        plusBtn.addEventListener("click", () => {
            const currentValue = parseInt(quantityInput.value)
            if (currentValue < parseInt(quantityInput.max)) {
                quantityInput.value = currentValue + 1
            }
        })
    }

    // Star rating functionality
    const starRating = document.querySelectorAll(".star-rating i")
    let selectedRating = 0

    starRating.forEach((star) => {
        star.addEventListener("mouseover", () => {
            const rating = Number.parseInt(star.getAttribute("data-rating"))
            highlightStars(rating)
        })

        star.addEventListener("mouseout", () => {
            highlightStars(selectedRating)
        })

        star.addEventListener("click", () => {
            selectedRating = Number.parseInt(star.getAttribute("data-rating"))
            highlightStars(selectedRating)
        })
    })

    function highlightStars(rating) {
        starRating.forEach((star) => {
            const starRating = Number.parseInt(star.getAttribute("data-rating"))
            if (starRating <= rating) {
                star.classList.remove("far")
                star.classList.add("fas")
                star.classList.add("active")
            } else {
                star.classList.remove("fas")
                star.classList.remove("active")
                star.classList.add("far")
            }
        })
    }

    // Review form submission
    const reviewForm = document.getElementById("reviewForm")
    if (reviewForm) {
        reviewForm.addEventListener("submit", (e) => {
            e.preventDefault()

            const reviewContent = document.getElementById("reviewContent").value

            if (selectedRating === 0) {
                alert("Vui lòng chọn số sao đánh giá!")
                return
            }

            if (reviewContent.trim() === "") {
                alert("Vui lòng nhập nội dung đánh giá!")
                return
            }

            // Here you would typically send the review data to your server
            // For this example, we'll just show a success message
            alert("Cảm ơn bạn đã gửi đánh giá!")

            // Reset form
            reviewForm.reset()
            selectedRating = 0
            highlightStars(0)
        })
    }

    // Add to cart functionality
    const addToCartButton = document.querySelector(".btn-add-cart")
    if (addToCartButton) {
        addToCartButton.addEventListener("click", async () => {
            if (addToCartButton.disabled) return

            const productId = addToCartButton.getAttribute("data-product-id")
            const quantity = parseInt(quantityInput.value)
            
            // Hiệu ứng loading
            const originalText = addToCartButton.innerHTML
            addToCartButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang thêm...'
            addToCartButton.disabled = true
            
            try {
                const token = document.querySelector('input[name="__RequestVerificationToken"]').value
                const response = await fetch(`/Cart/AddToCart?productId=${productId}&quantity=${quantity}`, {
                    method: "POST",
                    headers: { 
                        "Content-Type": "application/json",
                        "X-Requested-With": "XMLHttpRequest",
                        "RequestVerificationToken": token
                    }
                })
                
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`)
                }
                
                const result = await response.json()
                if (result.success) {
                    // Hiệu ứng thành công
                    addToCartButton.innerHTML = '<i class="fas fa-check"></i> Đã thêm'
                    addToCartButton.classList.add("success")
                    
                    // Cập nhật số lượng giỏ hàng nếu có
                    if (result.cartCount) {
                        updateCartCount(result.cartCount)
                    }
                    
                    setTimeout(() => {
                        addToCartButton.innerHTML = originalText
                        addToCartButton.classList.remove("success")
                        addToCartButton.disabled = false
                    }, 2000)
                    
                    showToast("Thêm vào giỏ hàng thành công", "success")
                } else {
                    addToCartButton.innerHTML = originalText
                    addToCartButton.disabled = false
                    showToast(result.message || "Không thể thêm vào giỏ hàng", "error")
                }
            } catch (error) {
                console.error("Error adding to cart:", error)
                addToCartButton.innerHTML = originalText
                addToCartButton.disabled = false
                showToast("Lỗi khi thêm vào giỏ hàng", "error")
            }
        })
    }

    // Buy now functionality
    const buyNowButton = document.querySelector(".btn-buy-now")
    if (buyNowButton) {
        buyNowButton.addEventListener("click", async () => {
            if (buyNowButton.disabled) return

            const productId = buyNowButton.getAttribute("data-product-id")
            const quantity = parseInt(quantityInput.value)
            
            // Hiệu ứng loading
            const originalText = buyNowButton.innerHTML
            buyNowButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang xử lý...'
            buyNowButton.disabled = true
            
            try {
                const token = document.querySelector('input[name="__RequestVerificationToken"]').value
                const response = await fetch("/Cart/AddToCart", {
                    method: "POST",
                    headers: { 
                        "Content-Type": "application/json",
                        "RequestVerificationToken": token
                    },
                    body: JSON.stringify({ productId: parseInt(productId), quantity: quantity })
                })
                
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`)
                }
                
                const result = await response.json()
                if (result.success) {
                    // Cập nhật số lượng giỏ hàng trước khi chuyển hướng
                    updateCartCount(result.cartCount)
                    window.location.href = "/Cart/Checkout"
                } else {
                    buyNowButton.innerHTML = originalText
                    buyNowButton.disabled = false
                    showToast(result.message, "error")
                }
            } catch (error) {
                console.error("Error processing buy now:", error)
                buyNowButton.innerHTML = originalText
                buyNowButton.disabled = false
                showToast("Lỗi khi xử lý đơn hàng", "error")
            }
        })
    }
})

// Function to update cart count
function updateCartCount(count) {
    const cartCountElements = document.querySelectorAll(".cart-count")
    cartCountElements.forEach(element => {
        element.textContent = count
        element.classList.add("updated")
        setTimeout(() => element.classList.remove("updated"), 1000)
    })
}

// Toast notification function
function showToast(message, type) {
    const toast = document.createElement("div")
    toast.className = `toast toast-${type}`
    toast.innerHTML = `
        <div class="toast-content">
            <i class="fas ${type === 'success' ? 'fa-check-circle' : 'fa-exclamation-circle'}"></i>
            <span>${message}</span>
        </div>
    `
    
    document.body.appendChild(toast)
    
    setTimeout(() => {
        toast.classList.add("show")
    }, 100)
    
    setTimeout(() => {
        toast.classList.remove("show")
        setTimeout(() => {
            document.body.removeChild(toast)
        }, 300)
    }, 3000)
}

// Add styles if not exists
if (!document.getElementById("cart-styles")) {
    const style = document.createElement("style")
    style.id = "cart-styles"
    style.textContent = `
        .toast {
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 12px 20px;
            border-radius: 4px;
            color: white;
            opacity: 0;
            transform: translateY(-20px);
            transition: opacity 0.3s, transform 0.3s;
            z-index: 9999;
            max-width: 300px;
        }
        .toast.show {
            opacity: 1;
            transform: translateY(0);
        }
        .toast-success {
            background-color: #28a745;
        }
        .toast-error {
            background-color: #dc3545;
        }
        .toast-content {
            display: flex;
            align-items: center;
        }
        .toast-content i {
            margin-right: 10px;
        }
        .btn-add-cart.success {
            background-color: #28a745;
            border-color: #28a745;
        }
        .quantity-control {
            display: flex;
            align-items: center;
            margin-bottom: 1rem;
        }
        .quantity-btn {
            width: 30px;
            height: 30px;
            border: 1px solid #ddd;
            background: #f8f9fa;
            cursor: pointer;
        }
        .quantity-btn:hover {
            background: #e9ecef;
        }
        .quantity-control input {
            width: 50px;
            height: 30px;
            text-align: center;
            border: 1px solid #ddd;
            margin: 0 5px;
        }
        .cart-count {
            transition: transform 0.3s;
        }
        .cart-count.updated {
            transform: scale(1.2);
        }
    `
    document.head.appendChild(style)
}
