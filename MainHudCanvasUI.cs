// Other code...

    // HP Bar Update
    float fillAmount = 0f; // Change from 1f to 0f for initial bar state
    hpBar.fillAmount = fillAmount;

    // Continue with existing logic for updating HP and XP bars
    fillAmount = currentHP / maxHP;
    hpBar.fillAmount = fillAmount;
    
    fillAmount = currentXP / maxXP;
    xpBar.fillAmount = fillAmount;

// Other code...