// Wait for DOM to load
document.addEventListener('DOMContentLoaded', function () {
    // Initialize all interactive features
    initRoleTabs();
    initFAQ();
    initSmoothScroll();
    initCalculator();
    initNavScroll();
    initCopilotSuggestions();
});

// Role Tabs Functionality
function initRoleTabs() {
    const tabs = document.querySelectorAll('.role-tab');
    const contents = document.querySelectorAll('.role-content');

    tabs.forEach(tab => {
        tab.addEventListener('click', function () {
            const role = this.getAttribute('data-role');

            // Remove active class from all tabs
            tabs.forEach(t => t.classList.remove('active'));

            // Add active class to clicked tab
            this.classList.add('active');

            // Hide all content
            contents.forEach(c => c.classList.remove('active'));

            // Show selected content
            const targetContent = document.querySelector(`.role-content[data-role="${role}"]`);
            if (targetContent) {
                targetContent.classList.add('active');
            }
        });
    });
}

// FAQ Accordion Functionality
function initFAQ() {
    const faqItems = document.querySelectorAll('.faq-item');

    faqItems.forEach(item => {
        const question = item.querySelector('.faq-question');

        question.addEventListener('click', function () {
            // Toggle active state
            const isActive = item.classList.contains('active');

            // Close all FAQ items
            faqItems.forEach(i => i.classList.remove('active'));

            // If this item wasn't active, open it
            if (!isActive) {
                item.classList.add('active');
            }
        });
    });
}

// Smooth Scrolling for Anchor Links
function initSmoothScroll() {
    const links = document.querySelectorAll('a[href^="#"]');

    links.forEach(link => {
        link.addEventListener('click', function (e) {
            const href = this.getAttribute('href');

            // Don't scroll for empty hash or just "#"
            if (!href || href === '#') {
                e.preventDefault();
                return;
            }

            const target = document.querySelector(href);

            if (target) {
                e.preventDefault();
                const offsetTop = target.offsetTop - 80; // Account for fixed navbar

                window.scrollTo({
                    top: offsetTop,
                    behavior: 'smooth'
                });
            }
        });
    });
}

// Salary Calculator
function initCalculator() {
    const calculateBtn = document.querySelector('.btn-calculate');

    if (calculateBtn) {
        calculateBtn.addEventListener('click', calculateSalary);

        // Also calculate on input change
        const inputs = document.querySelectorAll('.calculator-input input, .calculator-input select');
        inputs.forEach(input => {
            input.addEventListener('change', calculateSalary);
        });
    }
}

function calculateSalary() {
    // Get input values
    const salaireBrut = parseFloat(document.getElementById('salaire-brut').value) || 0;
    const anciennete = parseFloat(document.getElementById('anciennete').value) || 0;
    const enfants = parseFloat(document.getElementById('enfants').value) || 0;
    const statut = document.getElementById('statut').value;

    // Calculate prime d'ancienneté
    let primeAnciennete = 0;
    if (anciennete >= 2 && anciennete < 5) {
        primeAnciennete = salaireBrut * 0.05;
    } else if (anciennete >= 5 && anciennete < 12) {
        primeAnciennete = salaireBrut * 0.10;
    } else if (anciennete >= 12 && anciennete < 20) {
        primeAnciennete = salaireBrut * 0.15;
    } else if (anciennete >= 20 && anciennete < 25) {
        primeAnciennete = salaireBrut * 0.20;
    } else if (anciennete >= 25) {
        primeAnciennete = salaireBrut * 0.25;
    }

    // Calculate CNSS salarié (4.48%)
    const cnssSalarie = salaireBrut * 0.0448;

    // Calculate AMO salarié (2.26%)
    const amoSalarie = salaireBrut * 0.0226;

    // Calculate base imposable
    const salaireImposable = salaireBrut + primeAnciennete - cnssSalarie - amoSalarie;

    // Calculate IR (simplified progressive tax)
    let ir = 0;
    let deductions = 0;

    // Family deductions
    if (statut === 'marie') {
        deductions += 360; // Annual deduction for spouse
    }
    deductions += enfants * 360; // Annual deduction per child (max 6)
    deductions = Math.min(deductions, 2160); // Max 2160 MAD/year
    deductions = deductions / 12; // Monthly deduction

    const baseIR = salaireImposable - deductions;

    // Progressive tax brackets (2026 simplified)
    if (baseIR <= 2500) {
        ir = 0;
    } else if (baseIR <= 4166.67) {
        ir = (baseIR - 2500) * 0.10;
    } else if (baseIR <= 5000) {
        ir = 166.67 + (baseIR - 4166.67) * 0.20;
    } else if (baseIR <= 6666.67) {
        ir = 333.33 + (baseIR - 5000) * 0.30;
    } else if (baseIR <= 15000) {
        ir = 833.33 + (baseIR - 6666.67) * 0.34;
    } else {
        ir = 3666.67 + (baseIR - 15000) * 0.38;
    }

    // Calculate net à payer
    const netAPayer = salaireBrut + primeAnciennete - cnssSalarie - amoSalarie - ir;

    // Calculate employer costs
    const cnssPatronal = salaireBrut * 0.2109;
    const amoPatronal = salaireBrut * 0.0411;
    const coutTotalEmployeur = salaireBrut + primeAnciennete + cnssPatronal + amoPatronal;

    // Update UI
    updateCalculatorResults({
        salaireBrut,
        primeAnciennete,
        cnssSalarie,
        amoSalarie,
        baseIR: salaireImposable,
        ir,
        netAPayer,
        coutTotalEmployeur,
        cnssPatronal,
        amoPatronal
    });
}

function updateCalculatorResults(results) {
    const formatNumber = (num) => num.toLocaleString('fr-MA', { maximumFractionDigits: 0 });

    // Update result breakdown
    const resultLines = document.querySelectorAll('.result-line, .result-total, .result-employer');

    resultLines[0].querySelector('.amount').textContent = `${formatNumber(results.salaireBrut)} MAD`;
    resultLines[1].querySelector('.amount').textContent = `+ ${formatNumber(results.primeAnciennete)} MAD`;
    resultLines[2].querySelector('.amount').textContent = `– ${formatNumber(results.cnssSalarie)} MAD`;
    resultLines[3].querySelector('.amount').textContent = `– ${formatNumber(results.amoSalarie)} MAD`;
    resultLines[4].querySelector('.amount').textContent = `${formatNumber(results.baseIR)} MAD`;
    resultLines[5].querySelector('.amount').textContent = `– ${formatNumber(results.ir)} MAD`;
    resultLines[6].querySelector('.amount-total').textContent = `${formatNumber(results.netAPayer)} MAD`;
    resultLines[7].querySelector('.amount').textContent = `${formatNumber(results.coutTotalEmployeur)} MAD`;

    // Update cotisations detail
    const cotisationLines = document.querySelectorAll('.cotisation-line');
    cotisationLines[0].querySelector('.amount').textContent = `${formatNumber(results.cnssSalarie)} MAD`;
    cotisationLines[1].querySelector('.amount').textContent = `${formatNumber(results.amoSalarie)} MAD`;
    cotisationLines[2].querySelector('.amount').textContent = `${formatNumber(results.cnssPatronal)} MAD`;
    cotisationLines[3].querySelector('.amount').textContent = `${formatNumber(results.amoPatronal)} MAD`;
}

// Navbar Scroll Effect
function initNavScroll() {
    const navbar = document.querySelector('.navbar');
    let lastScroll = 0;

    window.addEventListener('scroll', function () {
        const currentScroll = window.pageYOffset;

        if (currentScroll > 100) {
            navbar.style.boxShadow = 'var(--shadow-md)';
        } else {
            navbar.style.boxShadow = 'none';
        }

        lastScroll = currentScroll;
    });
}

// Copilot Suggestions Interaction
function initCopilotSuggestions() {
    const suggestionBtns = document.querySelectorAll('.suggestion-btn');
    const chatContainer = document.querySelector('.copilot-chat');

    suggestionBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            const question = this.textContent;

            // Add user message
            const userMessage = createChatMessage('user', question);
            chatContainer.appendChild(userMessage);

            // Simulate AI response after a delay
            setTimeout(() => {
                const response = generateCopilotResponse(question);
                const aiMessage = createChatMessage('bot', response);
                chatContainer.appendChild(aiMessage);

                // Scroll to bottom
                chatContainer.scrollTop = chatContainer.scrollHeight;
            }, 800);
        });
    });
}

function createChatMessage(type, text) {
    const messageDiv = document.createElement('div');
    messageDiv.className = `chat-message ${type}`;

    const avatar = document.createElement('div');
    avatar.className = 'message-avatar';
    avatar.textContent = type === 'bot' ? '🤖' : '👤';

    const content = document.createElement('div');
    content.className = 'message-content';

    const p = document.createElement('p');
    p.textContent = text;

    content.appendChild(p);
    messageDiv.appendChild(avatar);
    messageDiv.appendChild(content);

    return messageDiv;
}

function generateCopilotResponse(question) {
    const responses = {
        "Quels sont les taux IR 2026 ?": "Les taux IR 2026 au Maroc suivent un barème progressif : 0% jusqu'à 30 000 MAD/an, 10% de 30 001 à 50 000 MAD, 20% de 50 001 à 60 000 MAD, 30% de 60 001 à 80 000 MAD, 34% de 80 001 à 180 000 MAD, et 38% au-delà de 180 000 MAD.",
        "Calcule la CNSS pour 15 000 MAD brut": "Pour un salaire brut de 15 000 MAD : CNSS salarié = 672 MAD (4.48%), CNSS patronal = 3 164 MAD (21.09%). Total cotisations CNSS = 3 836 MAD.",
        "Audite la paie de mars 2026": "Audit de mars 2026 : ✓ 47 bulletins conformes, ✓ Cotisations CNSS/AMO à jour, ✓ IR correctement calculé, ✓ Fichier Damancom prêt. Aucune anomalie détectée.",
        "Génère le fichier Damancom": "Fichier Damancom généré avec succès pour mars 2026. Contient : 47 salariés, masse salariale 284 000 MAD, cotisations totales 89 230 MAD. Prêt au dépôt."
    };

    return responses[question] || "Je traite votre demande. Les calculs sont en cours selon les taux 2026 en vigueur.";
}

// Intersection Observer for Animations on Scroll
const observerOptions = {
    threshold: 0.1,
    rootMargin: '0px 0px -100px 0px'
};

const observer = new IntersectionObserver(function (entries) {
    entries.forEach(entry => {
        if (entry.isIntersecting) {
            entry.target.style.animation = 'fadeInUp 0.8s ease-out forwards';
            observer.unobserve(entry.target);
        }
    });
}, observerOptions);

// Observe elements that should animate on scroll
document.addEventListener('DOMContentLoaded', function () {
    const animateElements = document.querySelectorAll('.problem-card, .step-card, .feature-card, .compliance-card, .testimonial-card, .pricing-card');

    animateElements.forEach(el => {
        el.style.opacity = '0';
        observer.observe(el);
    });
});

// Add active state to nav links based on scroll position
window.addEventListener('scroll', function () {
    const sections = document.querySelectorAll('section[id]');
    const navLinks = document.querySelectorAll('.nav-links a');

    let current = '';

    sections.forEach(section => {
        const sectionTop = section.offsetTop;
        const sectionHeight = section.clientHeight;

        if (window.pageYOffset >= sectionTop - 200) {
            current = section.getAttribute('id');
        }
    });

    navLinks.forEach(link => {
        link.classList.remove('active');
        if (link.getAttribute('href').includes(current) && current !== '') {
            link.classList.add('active');
        }
    });
});

// Mobile Menu Toggle (if needed in future)
function initMobileMenu() {
    const menuToggle = document.querySelector('.mobile-menu-toggle');
    const navLinks = document.querySelector('.nav-links');

    if (menuToggle) {
        menuToggle.addEventListener('click', function () {
            navLinks.classList.toggle('active');
        });
    }
}

// Form Validation for Sign Up / Contact Forms (if added)
function validateEmail(email) {
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
}

// Add loading state to buttons
function addLoadingState(button, isLoading) {
    if (isLoading) {
        button.disabled = true;
        button.textContent = 'Chargement...';
        button.style.opacity = '0.7';
    } else {
        button.disabled = false;
        button.textContent = button.getAttribute('data-original-text') || 'Envoyer';
        button.style.opacity = '1';
    }
}

// Export functions for potential use elsewhere
window.PayzenHR = {
    calculateSalary,
    updateCalculatorResults,
    validateEmail,
    addLoadingState
};