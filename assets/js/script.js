// BANNER CAROUSEL
let currentSlide = 0;
const slides = document.querySelectorAll(".carousel-slide");
const dots = document.querySelectorAll(".dot");
const prevBtn = document.getElementById("prevBtn");
const nextBtn = document.getElementById("nextBtn");

function showSlide(index) {
  slides.forEach((slide) => slide.classList.remove("active"));
  dots.forEach((dot) => dot.classList.remove("active"));
  
  slides[index].classList.add("active");
  dots[index].classList.add("active");
}

function nextSlide() {
  currentSlide = (currentSlide + 1) % slides.length;
  showSlide(currentSlide);
}

function prevSlide() {
  currentSlide = (currentSlide - 1 + slides.length) % slides.length;
  showSlide(currentSlide);
}

if (prevBtn && nextBtn) {
  prevBtn.addEventListener("click", prevSlide);
  nextBtn.addEventListener("click", nextSlide);
}

dots.forEach((dot) => {
  dot.addEventListener("click", function () {
    currentSlide = parseInt(this.getAttribute("data-slide"));
    showSlide(currentSlide);
  });
});

// Auto play carousel every 5 seconds
setInterval(nextSlide, 5000);

// MENU MOBILE
const menuToggle = document.getElementById("menuToggle");
const navMenu = document.getElementById("navMenu");

if (menuToggle && navMenu) {
  menuToggle.addEventListener("click", function () {
    navMenu.classList.toggle("show");
  });
}

// BACK TO TOP
const backToTop = document.getElementById("backToTop");

window.addEventListener("scroll", function () {
  if (window.scrollY > 300) {
    backToTop.style.display = "block";
  } else {
    backToTop.style.display = "none";
  }
});

if (backToTop) {
  backToTop.addEventListener("click", function () {
    window.scrollTo({
      top: 0,
      behavior: "smooth"
    });
  });
}

// FILTER SẢN PHẨM
const filterButtons = document.querySelectorAll(".filter-btn");
const filterItems = document.querySelectorAll(".filter-item");

if (filterButtons.length > 0) {
  filterButtons.forEach(button => {
    button.addEventListener("click", function () {
      filterButtons.forEach(btn => btn.classList.remove("active"));
      this.classList.add("active");

      const filterValue = this.getAttribute("data-filter");

      filterItems.forEach(item => {
        if (filterValue === "all" || item.classList.contains(filterValue)) {
          item.style.display = "block";
        } else {
          item.style.display = "none";
        }
      });
    });
  });
}

// FORM VALIDATION
const contactForm = document.getElementById("contactForm");

if (contactForm) {
  contactForm.addEventListener("submit", function (e) {
    e.preventDefault();

    const name = document.getElementById("name").value.trim();
    const email = document.getElementById("email").value.trim();
    const phone = document.getElementById("phone").value.trim();
    const message = document.getElementById("message").value.trim();

    const nameError = document.getElementById("nameError");
    const emailError = document.getElementById("emailError");
    const phoneError = document.getElementById("phoneError");
    const messageError = document.getElementById("messageError");
    const successMessage = document.getElementById("successMessage");

    nameError.textContent = "";
    emailError.textContent = "";
    phoneError.textContent = "";
    messageError.textContent = "";
    successMessage.textContent = "";

    let isValid = true;

    if (name === "") {
      nameError.textContent = "Vui lòng nhập họ tên";
      isValid = false;
    }

    const emailPattern = /^[^ ]+@[^ ]+\.[a-z]{2,3}$/;
    if (email === "") {
      emailError.textContent = "Vui lòng nhập email";
      isValid = false;
    } else if (!emailPattern.test(email)) {
      emailError.textContent = "Email không hợp lệ";
      isValid = false;
    }

    const phonePattern = /^[0-9]{10}$/;
    if (phone === "") {
      phoneError.textContent = "Vui lòng nhập số điện thoại";
      isValid = false;
    } else if (!phonePattern.test(phone)) {
      phoneError.textContent = "Số điện thoại phải gồm 10 chữ số";
      isValid = false;
    }

    if (message === "") {
      messageError.textContent = "Vui lòng nhập nội dung";
      isValid = false;
    }

    if (isValid) {
      successMessage.textContent = "Gửi thông tin thành công!";
      contactForm.reset();
    }
  });
}
// DATA CHI TIẾT HONDA VISION
const visionData = {
  thethao: {
    versionName: "Thể thao",
    price: "36.600.000 VNĐ",
    colors: [
      {
        name: "Bạc đen",
        code: "linear-gradient(90deg, #d9d9d9 50%, #111 50%)",
        image: "images/vision-bac-den.png"
      },
      {
        name: "Đen",
        code: "#111",
        image: "images/vision-den.png"
      },
      {
        name: "Xám đen",
        code: "linear-gradient(90deg, #9f9f9f 50%, #111 50%)",
        image: "images/vision-xam-den.png"
      }
    ]
  }
};

const versionButtons = document.querySelectorAll(".version-btn");
const colorList = document.getElementById("colorList");
const visionMainImage = document.getElementById("visionMainImage");
const visionPrice = document.getElementById("visionPrice");
const visionVersionName = document.getElementById("visionVersionName");
const visionColorText = document.getElementById("visionColorText");
const selectedColorName = document.getElementById("selectedColorName");

function renderVisionColors(versionKey) {
  if (!colorList || !visionMainImage || !visionPrice || !visionVersionName || !visionColorText || !selectedColorName) return;

  const version = visionData[versionKey];
  colorList.innerHTML = "";

  visionPrice.textContent = version.price;
  visionVersionName.textContent = version.versionName;

  version.colors.forEach((color, index) => {
    const colorItem = document.createElement("div");
    colorItem.classList.add("color-item");
    if (index === 0) colorItem.classList.add("active");

    colorItem.innerHTML = `
      <div class="color-swatch"></div>
      <div class="color-name">${color.name}</div>
    `;

    const swatch = colorItem.querySelector(".color-swatch");
    swatch.style.background = color.code;

    colorItem.addEventListener("click", function () {
      document.querySelectorAll(".color-item").forEach(item => item.classList.remove("active"));
      colorItem.classList.add("active");

      visionMainImage.src = color.image;
      visionMainImage.alt = "Honda Vision " + color.name;
      visionColorText.textContent = color.name;
      selectedColorName.textContent = color.name;
    });

    colorList.appendChild(colorItem);
  });

  visionMainImage.src = version.colors[0].image;
  visionMainImage.alt = "Honda Vision " + version.colors[0].name;
  visionColorText.textContent = version.colors[0].name;
  selectedColorName.textContent = version.colors[0].name;
}

if (versionButtons.length > 0) {
  versionButtons.forEach(button => {
    button.addEventListener("click", function () {
      versionButtons.forEach(btn => btn.classList.remove("active"));
      this.classList.add("active");

      const versionKey = this.getAttribute("data-version");
      renderVisionColors(versionKey);
    });
  });

  renderVisionColors("thethao");
}
// TÍNH NĂNG NỔI BẬT KIỂU HONDA
const featureMainImage = document.getElementById("featureMainImage");
const featureTitle = document.getElementById("featureTitle");
const featureDesc = document.getElementById("featureDesc");
const featureThumbs = document.querySelectorAll(".feature-thumb");

if (featureThumbs.length > 0) {
  featureThumbs.forEach((thumb) => {
    thumb.addEventListener("click", function () {
      featureThumbs.forEach((item) => item.classList.remove("active"));
      this.classList.add("active");

      const newImage = this.getAttribute("data-image");
      const newTitle = this.getAttribute("data-title");
      const newDesc = this.getAttribute("data-desc");

      if (featureMainImage) featureMainImage.src = newImage;
      if (featureTitle && newTitle) featureTitle.textContent = newTitle;
      if (featureDesc && newDesc) featureDesc.textContent = newDesc;
    });
  });
}
// DATA CHI TIẾT HONDA AIR BLADE
const airbladeData = {
  tieuchuan: {
    versionName: "Tiêu chuẩn",
    price: "56.000.000 VNĐ",
    colors: [
      {
        name: "Đỏ",
        code: "#c40000",
        image: "images/airblade-do.png"
      },
      {
        name: "Đen",
        code: "#111",
        image: "images/airblade-den.png"
      }
    ]
  },
  thethao: {
    versionName: "Thể thao",
    price: "57.500.000 VNĐ",
    colors: [
      {
        name: "Trắng đỏ",
        code: "linear-gradient(90deg, #f3f3f3 50%, #c40000 50%)",
        image: "images/airblade-trang-do.png"
      }
    ]
  }
};

const airbladeVersionButtons = document.querySelectorAll("[data-airblade-version]");
const airbladeColorList = document.getElementById("airbladeColorList");
const airbladeMainImage = document.getElementById("airbladeMainImage");
const airbladePrice = document.getElementById("airbladePrice");
const airbladeVersionName = document.getElementById("airbladeVersionName");
const airbladeColorText = document.getElementById("airbladeColorText");
const airbladeSelectedColorName = document.getElementById("airbladeSelectedColorName");

function renderAirbladeColors(versionKey) {
  if (
    !airbladeColorList ||
    !airbladeMainImage ||
    !airbladePrice ||
    !airbladeVersionName ||
    !airbladeColorText ||
    !airbladeSelectedColorName
  ) return;

  const version = airbladeData[versionKey];
  airbladeColorList.innerHTML = "";

  airbladePrice.textContent = version.price;
  airbladeVersionName.textContent = version.versionName;

  version.colors.forEach((color, index) => {
    const colorItem = document.createElement("div");
    colorItem.classList.add("color-item");
    if (index === 0) colorItem.classList.add("active");

    colorItem.innerHTML = `
      <div class="color-swatch"></div>
      <div class="color-name">${color.name}</div>
    `;

    const swatch = colorItem.querySelector(".color-swatch");
    swatch.style.background = color.code;

    colorItem.addEventListener("click", function () {
      document.querySelectorAll("#airbladeColorList .color-item").forEach(item => {
        item.classList.remove("active");
      });

      colorItem.classList.add("active");
      airbladeMainImage.src = color.image;
      airbladeMainImage.alt = "Honda Air Blade " + color.name;
      airbladeColorText.textContent = color.name;
      airbladeSelectedColorName.textContent = color.name;
    });

    airbladeColorList.appendChild(colorItem);
  });

  airbladeMainImage.src = version.colors[0].image;
  airbladeMainImage.alt = "Honda Air Blade " + version.colors[0].name;
  airbladeColorText.textContent = version.colors[0].name;
  airbladeSelectedColorName.textContent = version.colors[0].name;
}

if (airbladeVersionButtons.length > 0) {
  airbladeVersionButtons.forEach(button => {
    button.addEventListener("click", function () {
      airbladeVersionButtons.forEach(btn => btn.classList.remove("active"));
      this.classList.add("active");

      const versionKey = this.getAttribute("data-airblade-version");
      renderAirbladeColors(versionKey);
    });
  });

  renderAirbladeColors("tieuchuan");
}

// DATA CHI TIẾT HONDA WINNER R
const winnerData = {
  dactrung: {
    versionName: "Đặc biệt",
    price: "50.560.000 VNĐ",
    colors: [
      {
        name: "Xanh Đen",
        code: "linear-gradient(90deg, #0f3b5c 45%, #111 55%)",
        image: "images/winner-xanh-den.png"
      },
      {
        name: "Đỏ Đen",
        code: "linear-gradient(90deg, #c40000 45%, #111 55%)",
        image: "images/winner-do-den.png"
      },
      {
        name: "Xám Đen",
        code: "linear-gradient(90deg, #808080 45%, #111 55%)",
        image: "images/winner-xam-den.png"
      },
      {
        name: "Đen",
        code: "#111",
        image: "images/winner-den.png"
      }
    ]
  },
  tieuchuan: {
    versionName: "Tiêu chuẩn",
    price: "48.000.000 VNĐ",
    colors: [
      {
        name: "Đen Bạc",
        code: "linear-gradient(90deg, #bbb 50%, #111 50%)",
        image: "images/winner-den-bac.png"
      },
      {
        name: "Đỏ Đen",
        code: "linear-gradient(90deg, #c40000 45%, #111 55%)",
        image: "images/winner-do-den.png"
      }
    ]
  }
};

const winnerVersionButtons = document.querySelectorAll("[data-winner-version]");
const winnerColorList = document.getElementById("winnerColorList");
const winnerMainImage = document.getElementById("winnerMainImage");
const winnerPrice = document.getElementById("winnerPrice");
const winnerVersionName = document.getElementById("winnerVersionName");
const winnerColorText = document.getElementById("winnerColorText");
const winnerSelectedColorName = document.getElementById("winnerSelectedColorName");

function renderWinnerColors(versionKey) {
  if (
    !winnerColorList ||
    !winnerMainImage ||
    !winnerPrice ||
    !winnerVersionName ||
    !winnerColorText ||
    !winnerSelectedColorName
  ) return;

  const version = winnerData[versionKey];
  winnerColorList.innerHTML = "";

  winnerPrice.textContent = version.price;
  winnerVersionName.textContent = version.versionName;

  version.colors.forEach((color, index) => {
    const colorItem = document.createElement("div");
    colorItem.classList.add("color-item");
    if (index === 0) colorItem.classList.add("active");

    colorItem.innerHTML = `
      <div class="color-swatch"></div>
      <div class="color-name">${color.name}</div>
    `;

    const swatch = colorItem.querySelector(".color-swatch");
    swatch.style.background = color.code;

    colorItem.addEventListener("click", function () {
      document.querySelectorAll("#winnerColorList .color-item").forEach(item => {
        item.classList.remove("active");
      });

      colorItem.classList.add("active");
      winnerMainImage.src = color.image;
      winnerMainImage.alt = "Honda Winner R " + color.name;
      winnerColorText.textContent = color.name;
      winnerSelectedColorName.textContent = color.name;
    });

    winnerColorList.appendChild(colorItem);
  });

  winnerMainImage.src = version.colors[0].image;
  winnerMainImage.alt = "Honda Winner R " + version.colors[0].name;
  winnerColorText.textContent = version.colors[0].name;
  winnerSelectedColorName.textContent = version.colors[0].name;
}

if (winnerVersionButtons.length > 0) {
  winnerVersionButtons.forEach(button => {
    button.addEventListener("click", function () {
      winnerVersionButtons.forEach(btn => btn.classList.remove("active"));
      this.classList.add("active");

      const versionKey = this.getAttribute("data-winner-version");
      renderWinnerColors(versionKey);
    });
  });

  renderWinnerColors("dactrung");
}

// DATA CHI TIẾT YAMAHA YZF-R15
const yzfData = {
  colors: [
    {
      name: "Xanh dương",
      code: "linear-gradient(90deg, #1a90f0 50%, #0b4ecf 50%)",
      image: "images/YZF-R15.png"
    },
    {
      name: "Đen",
      code: "#111",
      image: "images/YZF-R15-den.png"
    }
  ]
};

const yzfColorList = document.getElementById("yzfColorList");
const yzfMainImage = document.getElementById("yzfMainImage");
const yzfPrice = document.getElementById("yzfPrice");
const yzfColorText = document.getElementById("yzfColorText");
const yzfSelectedColorName = document.getElementById("yzfSelectedColorName");

function renderYzfColors() {
  if (!yzfColorList || !yzfMainImage || !yzfPrice || !yzfColorText || !yzfSelectedColorName) return;

  yzfColorList.innerHTML = "";

  yzfData.colors.forEach((color, index) => {
    const colorItem = document.createElement("div");
    colorItem.classList.add("color-item");
    if (index === 0) colorItem.classList.add("active");

    colorItem.innerHTML = `
      <div class="color-swatch"></div>
      <div class="color-name">${color.name}</div>
    `;

    const swatch = colorItem.querySelector(".color-swatch");
    swatch.style.background = color.code;

    colorItem.addEventListener("click", function () {
      document.querySelectorAll("#yzfColorList .color-item").forEach(item => {
        item.classList.remove("active");
      });

      colorItem.classList.add("active");
      yzfMainImage.src = color.image;
      yzfMainImage.alt = "Yamaha YZF-R15 " + color.name;
      yzfColorText.textContent = color.name;
      yzfSelectedColorName.textContent = color.name;
    });

    yzfColorList.appendChild(colorItem);
  });

  yzfMainImage.src = yzfData.colors[0].image;
  yzfMainImage.alt = "Yamaha YZF-R15 " + yzfData.colors[0].name;
  yzfColorText.textContent = yzfData.colors[0].name;
  yzfSelectedColorName.textContent = yzfData.colors[0].name;
}

// Gọi hàm render nếu có element YZF-R15
if (yzfColorList) {
  renderYzfColors();
}

// DATA CHI TIẾT EVO GRAND
const evograndData = {
  colors: [
    {
      name: "Trắng",
      code: "#f3f3f3",
      image: "images/vin-evo-trang.png"
    },
    {
      name: "Đỏ",
      code: "#c40000",
      image: "images/vin-evo-do.png"
    },
    {
      name: "Xanh rêu",
      code: "#3a7578",
      image: "images/vin-evo-xanh.png"
    },
    {
      name: "Da",
      code: "#c9b5a0",
      image: "images/vin-evo-da.png"
    }
  ]
};

const evograndColorList = document.getElementById("evograndColorList");
const evograndMainImage = document.getElementById("evograndMainImage");
const evograndColorText = document.getElementById("evograndColorText");
const evograndSelectedColorName = document.getElementById("evograndSelectedColorName");

function renderEvograndColors() {
  if (!evograndColorList || !evograndMainImage || !evograndColorText || !evograndSelectedColorName) return;

  evograndColorList.innerHTML = "";

  evograndData.colors.forEach((color, index) => {
    const colorItem = document.createElement("div");
    colorItem.classList.add("color-item");
    if (index === 0) colorItem.classList.add("active");

    colorItem.innerHTML = `
      <div class="color-swatch"></div>
      <div class="color-name">${color.name}</div>
    `;

    const swatch = colorItem.querySelector(".color-swatch");
    swatch.style.background = color.code;

    colorItem.addEventListener("click", function () {
      document.querySelectorAll("#evograndColorList .color-item").forEach(item => {
        item.classList.remove("active");
      });

      colorItem.classList.add("active");
      evograndMainImage.src = color.image;
      evograndMainImage.alt = "Evo Grand " + color.name;
      evograndColorText.textContent = color.name;
      evograndSelectedColorName.textContent = color.name;
    });

    evograndColorList.appendChild(colorItem);
  });

  evograndMainImage.src = evograndData.colors[0].image;
  evograndMainImage.alt = "Evo Grand " + evograndData.colors[0].name;
  evograndColorText.textContent = evograndData.colors[0].name;
  evograndSelectedColorName.textContent = evograndData.colors[0].name;
}

// Gọi hàm render nếu có element Evo Grand
if (evograndColorList) {
  renderEvograndColors();
}