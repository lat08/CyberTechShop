// Product slider functionality

document.addEventListener("DOMContentLoaded", () => {
    // Initialize product sliders
    const productSliders = document.querySelectorAll(".product-slider")

    productSliders.forEach((slider) => {
        // Create slider controls
        const sliderControls = document.createElement("div")
        sliderControls.className = "slider-controls"
        sliderControls.innerHTML = `
            <button class="slider-prev"><i class="fas fa-chevron-left"></i></button>
            <button class="slider-next"><i class="fas fa-chevron-right"></i></button>
        `

        // Add controls to slider
        slider.appendChild(sliderControls)

        // Get the existing slider track
        const sliderTrack = slider.querySelector(".slider-track")
        if (!sliderTrack) return; // Exit if,No track found

        // Ensure track styles
        sliderTrack.style.width = "100%"
        sliderTrack.style.display = "flex"
        sliderTrack.style.transition = "transform 0.3s ease"

        // Get slider controls
        const prevBtn = slider.querySelector(".slider-prev")
        const nextBtn = slider.querySelector(".slider-next")

        // Set initial position
        let position = 0
        const itemWidth = 200 // Width of each product card + margin
        const visibleItems = Math.floor((slider.offsetWidth - 20) / itemWidth)
        const maxPosition = Math.max(0, sliderTrack.children.length - visibleItems)

        // Update slider position
        function updateSliderPosition() {
            const maxTranslate = sliderTrack.children.length * itemWidth - slider.offsetWidth + 40
            const translateX = Math.min(position * itemWidth, maxTranslate)
            sliderTrack.style.transform = `translateX(${-translateX}px)`

            // Update button states
            prevBtn.disabled = position === 0
            nextBtn.disabled = position >= maxPosition

            // Update button appearance
            prevBtn.style.opacity = position === 0 ? "0.5" : "1"
            nextBtn.style.opacity = position >= maxPosition ? "0.5" : "1"
        }

        // Add event listeners to controls
        prevBtn.addEventListener("click", () => {
            if (position > 0) {
                position--
                updateSliderPosition()
            }
        })

        nextBtn.addEventListener("click", () => {
            if (position < maxPosition) {
                position++
                updateSliderPosition()
            }
        })

        // Initial update
        updateSliderPosition()

        // Update on window resize
        window.addEventListener("resize", () => {
            const newSliderWidth = slider.offsetWidth
            const newVisibleItems = Math.floor((newSliderWidth - 20) / itemWidth)
            const newMaxPosition = Math.max(0, sliderTrack.children.length - newVisibleItems)

            if (position > newMaxPosition) {
                position = newMaxPosition
            }

            updateSliderPosition()
        })
    })

    // Add slider styles
    const sliderStyles = document.createElement("style")
    sliderStyles.textContent = `
    .product-slider {
        position: relative;
        overflow: hidden;
        margin-bottom: 20px;
        width: 100%;
        max-width: 100%;
        box-sizing: border-box;
    }
    
    .slider-track {
        display: flex;
        transition: transform 0.3s ease;
        width: 100%;
        box-sizing: border-box;
    }
    
    .slider-track .proloop {
        flex: 0 0 180px;
        margin: 0 10px;
        max-width: 180px;
        box-sizing: border-box;
    }
    
    .slider-controls {
        position: absolute;
        top: 50%;
        left: 10px;
        right: 10px;
        transform: translateY(-50%);
        display: flex;
        justify-content: space-between;
        pointer-events: none;
        z-index: 10;
        width: calc(100% - 20px);
    }
    
    .slider-prev, .slider-next {
        width: 40px;
        height: 40px;
        background-color: white;
        border: none;
        border-radius: 50%;
        box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1);
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
        pointer-events: auto;
        transition: opacity 0.3s;
        z-index: 20;
    }
    
    .slider-prev:disabled, .slider-next:disabled {
        cursor: not-allowed;
    }
`
    document.head.appendChild(sliderStyles)
})