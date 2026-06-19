#ifndef LOGINWIDGET_H
#define LOGINWIDGET_H

#include <QWidget>
#include <QLineEdit>
#include <QPushButton>

class LoginWidget : public QWidget {
    Q_OBJECT

public:
    explicit LoginWidget(QWidget *parent = nullptr);

signals:
    void loginSuccess(const QString& userId, const QString& token);
    void registerRequested();

private slots:
    void onLogin();
    void onRegister();
    void toggleMode();

private:
    void setupUI();
    
    bool isLoginMode = true;
    
    QLineEdit* nameInput;
    QLineEdit* emailInput;
    QLineEdit* passInput;
    QPushButton* submitBtn;
    QPushButton* toggleBtn;
};

#endif