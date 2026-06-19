#include "loginwidget.h"
#include "lyfron/lyfron.h"
#include <QVBoxLayout>
#include <QLabel>
#include <QGraphicsDropShadowEffect>

LoginWidget::LoginWidget(QWidget *parent) : QWidget(parent) {
    setupUI();
}

void LoginWidget::setupUI() {
    auto* layout = new QVBoxLayout(this);
    layout->setAlignment(Qt::AlignCenter);
    
    // Logo
    auto* logo = new QLabel("⚡", this);
    logo->setStyleSheet("font-size: 64px; margin-bottom: 10px;");
    logo->setAlignment(Qt::AlignCenter);
    
    auto* title = new QLabel("RunApp", this);
    title->setStyleSheet(
        "font-size: 32px; font-weight: 800; color: #1877f2; margin-bottom: 5px;"
    );
    title->setAlignment(Qt::AlignCenter);
    
    auto* subtitle = new QLabel("Connect with your world", this);
    subtitle->setStyleSheet("color: #65676b; margin-bottom: 30px;");
    subtitle->setAlignment(Qt::AlignCenter);
    
    // Form card
    auto* card = new QWidget(this);
    card->setStyleSheet(
        "QWidget { background: #242526; border-radius: 12px; padding: 20px; }"
        "QLineEdit { background: #3a3b3c; border: 1px solid #3a3b3c; "
        "border-radius: 8px; padding: 12px; color: white; font-size: 15px; }"
        "QLineEdit:focus { border-color: #1877f2; }"
        "QPushButton { border-radius: 8px; padding: 12px; font-weight: 700; "
        "font-size: 16px; cursor: pointer; }"
    );
    card->setMaximumWidth(400);
    
    auto* shadow = new QGraphicsDropShadowEffect(card);
    shadow->setBlurRadius(20);
    shadow->setColor(QColor(0, 0, 0, 80));
    shadow->setOffset(0, 4);
    card->setGraphicsEffect(shadow);
    
    auto* cardLayout = new QVBoxLayout(card);
    cardLayout->setSpacing(12);
    
    nameInput = new QLineEdit(this);
    nameInput->setPlaceholderText("Full name");
    nameInput->setVisible(false);
    
    emailInput = new QLineEdit(this);
    emailInput->setPlaceholderText("Email or phone");
    
    passInput = new QLineEdit(this);
    passInput->setPlaceholderText("Password");
    passInput->setEchoMode(QLineEdit::Password);
    
    submitBtn = new QPushButton("Log In", this);
    submitBtn->setStyleSheet(
        "background: #1877f2; color: white; border: none;"
    );
    connect(submitBtn, &QPushButton::clicked, this, &LoginWidget::onLogin);
    
    toggleBtn = new QPushButton("Create New Account", this);
    toggleBtn->setStyleSheet(
        "background: #42b72a; color: white; border: none; margin-top: 10px;"
    );
    connect(toggleBtn, &QPushButton::clicked, this, &LoginWidget::toggleMode);
    
    cardLayout->addWidget(nameInput);
    cardLayout->addWidget(emailInput);
    cardLayout->addWidget(passInput);
    cardLayout->addWidget(submitBtn);
    cardLayout->addWidget(toggleBtn);
    
    layout->addWidget(logo);
    layout->addWidget(title);
    layout->addWidget(subtitle);
    layout->addWidget(card);
}

void LoginWidget::toggleMode() {
    isLoginMode = !isLoginMode;
    nameInput->setVisible(!isLoginMode);
    submitBtn->setText(isLoginMode ? "Log In" : "Sign Up");
    toggleBtn->setText(isLoginMode ? "Create New Account" : "Already have account?");
}

void LoginWidget::onLogin() {
    QString email = emailInput->text();
    QString pass = passInput->text();
    
    if (email.isEmpty() || pass.isEmpty()) return;
    
    // Hash password with Lyfron C layer
    auto& lyfron = Lyfron::SecurityEngine::instance();
    std::string hash = lyfron.hashPassword(pass.toStdString());
    
    // TODO: Send to PHP backend
    // For now, simulate success
    emit loginSuccess("user_123", "mock_jwt_token");
}

void LoginWidget::onRegister() {
    // Same flow with name included
    onLogin();
}