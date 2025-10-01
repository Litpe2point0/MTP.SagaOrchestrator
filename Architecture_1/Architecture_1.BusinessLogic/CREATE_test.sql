USE SurveyTalkDB_test

-- Role
CREATE TABLE Role (
    id INT PRIMARY KEY,
    name NVARCHAR(20) NOT NULL
);

-- Account
CREATE TABLE Account (
    id INT IDENTITY PRIMARY KEY,
    email VARCHAR(250) NOT NULL,
    password VARCHAR(250) NOT NULL,
    roleId INT NOT NULL REFERENCES Role(id),
    fullName NVARCHAR(250) NULL,
    dob DATE NULL,
    gender NVARCHAR(250) NULL,
    address NVARCHAR(250) NULL,
    phone VARCHAR(20) NULL,
    balance DECIMAL(18,2) NOT NULL DEFAULT(0),
    isVerified BIT NOT NULL DEFAULT(0),
    xp INT NOT NULL DEFAULT(0),
    level INT NOT NULL DEFAULT(1),
    progressionSurveyCount INT NOT NULL DEFAULT(0),
    isFilterSurveyRequired BIT NOT NULL,
    lastFilterSurveyTakenAt DATETIME NULL,
    deactivatedAt DATETIME NULL,
    createdAt DATETIME NOT NULL DEFAULT(GETDATE()),
    updatedAt DATETIME NOT NULL DEFAULT(GETDATE())
);

-- AccountOnlineTracking
CREATE TABLE AccountOnlineTracking (
    id INT IDENTITY PRIMARY KEY,
    accountId INT NOT NULL REFERENCES Account(id),
    onlineDate DATE NOT NULL DEFAULT(CAST(GETDATE() AS DATE)),
    surveyTakenCount INT NOT NULL DEFAULT(1)
);

-- AccountVerification
CREATE TABLE AccountVerification (
    accountId INT PRIMARY KEY REFERENCES Account(id),
    nationalCardNumber INT NOT NULL,
    expirationDate DATE NULL,
    verifiedAt DATETIME NOT NULL
);

-- AccountProfile
CREATE TABLE AccountProfile (
    accountId INT PRIMARY KEY REFERENCES Account(id),
    countryRegion NVARCHAR(250) NULL,
    maritalStatus NVARCHAR(250) NULL,
    averageIncome NVARCHAR(250) NULL,
    educationLevel NVARCHAR(250) NULL,
    jobField NVARCHAR(250) NULL,
    provinceCode INT NULL,
    districtCode INT NULL,
    wardCode INT NULL
);

-- SystemConfigProfile
CREATE TABLE SystemConfigProfile (
    id INT IDENTITY PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    description NVARCHAR(MAX) NULL,
    isActive BIT NOT NULL DEFAULT(0),
    createdAt DATETIME NOT NULL DEFAULT(GETDATE()),
    updatedAt DATETIME NOT NULL DEFAULT(GETDATE())
);

-- AccountGeneralConfig
CREATE TABLE AccountGeneralConfig (
    configProfileId INT PRIMARY KEY REFERENCES SystemConfigProfile(id),
    xpLevelThreshold INT NOT NULL,
    filterSurveyCycle INT NOT NULL,
    dailyActiveCountPeriod INT NOT NULL,
    safetyFilterRate FLOAT NOT NULL
);

-- AccountLevelSettingConfig
CREATE TABLE AccountLevelSettingConfig (
    configProfileId INT NOT NULL REFERENCES SystemConfigProfile(id),
    level INT NOT NULL,
    dailyReductionXp INT NOT NULL,
    progressionSurveyCount INT NOT NULL,
    bonusRate FLOAT NOT NULL,
    CONSTRAINT PK_AccountLevelSettingConfig PRIMARY KEY(configProfileId, level)
);

-- SurveyGeneralConfig
CREATE TABLE SurveyGeneralConfig (
    configProfileId INT PRIMARY KEY REFERENCES SystemConfigProfile(id),
    pricePerQuestion DECIMAL(18,2) NOT NULL,
    xpPerQuestion INT NOT NULL,
    publishProfitRate FLOAT NOT NULL,
    basePriceAllocationRate FLOAT NOT NULL,
    timePriceAllocationRate FLOAT NOT NULL,
    levelPriceAllocationRate FLOAT NOT NULL
);

-- SurveyTimeRateConfig
CREATE TABLE SurveyTimeRateConfig (
    id INT IDENTITY PRIMARY KEY,
    configProfileId INT NOT NULL REFERENCES SystemConfigProfile(id),
    minDurationRate FLOAT NOT NULL,
    maxDurationRate FLOAT NOT NULL,
    rate FLOAT NOT NULL
);

-- SurveyMarketConfig
CREATE TABLE SurveyMarketConfig (
    configProfileId INT PRIMARY KEY REFERENCES SystemConfigProfile(id),
    dataTransactionProfitRate FLOAT NOT NULL
);

-- SurveySecurityMode
CREATE TABLE SurveySecurityMode (
    id INT PRIMARY KEY,
    name NVARCHAR(50) NOT NULL,
    description NVARCHAR(MAX) NULL
);

-- SurveySecurityModeConfig
CREATE TABLE SurveySecurityModeConfig (
    configProfileId INT NOT NULL REFERENCES SystemConfigProfile(id),
    surveySecurityModeId INT NOT NULL REFERENCES SurveySecurityMode(id),
    rate FLOAT NOT NULL,
    CONSTRAINT PK_SurveySecurityModeConfig PRIMARY KEY(configProfileId, surveySecurityModeId)
);

-- SurveyStatus
CREATE TABLE SurveyStatus (
    id INT PRIMARY KEY,
    name NVARCHAR(50) NOT NULL
);

-- SurveyType
CREATE TABLE SurveyType (
    id INT PRIMARY KEY,
    name NVARCHAR(50) NOT NULL
);

-- SurveyTopic
CREATE TABLE SurveyTopic (
    id INT IDENTITY PRIMARY KEY,
    name NVARCHAR(50) NOT NULL
);

-- SurveySpecificTopic
CREATE TABLE SurveySpecificTopic (
    id INT IDENTITY PRIMARY KEY,
    name NVARCHAR(50) NOT NULL,
    surveyTopicId INT NOT NULL REFERENCES SurveyTopic(id)
);

-- SurveyQuestionType
CREATE TABLE SurveyQuestionType (
    id INT PRIMARY KEY,
    name NVARCHAR(50) NOT NULL,
    price DECIMAL(18,2) NOT NULL,
    deactivatedAt DATETIME NULL DEFAULT(GETDATE())
);

-- SurveyFieldInputType
CREATE TABLE SurveyFieldInputType (
    id INT PRIMARY KEY,
    name NVARCHAR(250) NOT NULL
);

-- Survey
CREATE TABLE Survey (
    id INT IDENTITY PRIMARY KEY,
    requesterId INT NOT NULL REFERENCES Account(id),
    title NVARCHAR(250) NOT NULL,
    description NVARCHAR(MAX) NULL,
    surveyTypeId INT NOT NULL REFERENCES SurveyType(id),
    surveyTopicId INT NULL REFERENCES SurveyTopic(id),
    surveySpecificTopicId INT NULL REFERENCES SurveySpecificTopic(id),
    startDate DATE NULL,
    endDate DATE NULL,
    kpi INT NULL,
    securityModeId INT NULL REFERENCES SurveySecurityMode(id),
    theoryPrice DECIMAL(18,2) NULL,
    extraPrice DECIMAL(18,2) NULL,
    takerBaseRewardPrice DECIMAL(18,2) NULL,
    profitPrice DECIMAL(18,2) NULL,
    allocBaseAmount DECIMAL(18,2) NULL,
    allocTimeAmount DECIMAL(18,2) NULL,
    allocLevelAmount DECIMAL(18,2) NULL,
    maxXp INT NULL,
    isAvailable BIT NOT NULL DEFAULT(0),
    configJsonString NVARCHAR(MAX) NOT NULL DEFAULT(''),
    publishedAt DATETIME NULL,
    deletedAt DATETIME NULL,
    createdAt DATETIME NOT NULL DEFAULT(GETDATE())
);

-- SurveyStatusTracking
CREATE TABLE SurveyStatusTracking (
    id INT IDENTITY PRIMARY KEY,
    surveyId INT NOT NULL REFERENCES Survey(id),
    surveyStatusId INT NOT NULL REFERENCES SurveyStatus(id),
    createdAt DATETIME NOT NULL DEFAULT(GETDATE())
);

-- SurveyQuestion
CREATE TABLE SurveyQuestion (
    id INT IDENTITY PRIMARY KEY,
    surveyId INT NOT NULL REFERENCES Survey(id),
    questionTypeId INT NOT NULL REFERENCES SurveyQuestionType(id),
    content NVARCHAR(500) NOT NULL,
    description NVARCHAR(500) NULL,
    timeLimit INT NOT NULL DEFAULT(10),
    isVoiced BIT NOT NULL DEFAULT(0),
    [order] TINYINT NOT NULL,
    configJsonString NVARCHAR(MAX) NOT NULL DEFAULT(''),
    deletedAt DATETIME NULL
);

-- SurveyOption
CREATE TABLE SurveyOption (
    id INT IDENTITY PRIMARY KEY,
    surveyQuestionId INT NOT NULL REFERENCES SurveyQuestion(id),
    content NVARCHAR(500) NOT NULL,
    [order] TINYINT NOT NULL
);

-- SurveyMarketVersionStatusTracking
CREATE TABLE SurveyMarketVersionStatusTracking (
    id INT IDENTITY PRIMARY KEY,
    surveyId INT NOT NULL REFERENCES Survey(id),
    version TINYINT NOT NULL,
    surveyStatusId INT NOT NULL REFERENCES SurveyStatus(id),
    createdAt DATETIME NOT NULL DEFAULT(GETDATE())
);

-- SurveyMarket
CREATE TABLE SurveyMarket (
    surveyId INT NOT NULL REFERENCES Survey(id),
    version TINYINT NOT NULL,
    description NVARCHAR(MAX) NULL,
    pricePerResponse DECIMAL(18,2) NULL,
    isAvailable BIT NOT NULL DEFAULT(0),
    expiredAt DATETIME NULL,
    publishAt DATETIME NULL,
    createdAt DATETIME NOT NULL DEFAULT(GETDATE()),
    updatedAt DATETIME NOT NULL DEFAULT(GETDATE()),
    CONSTRAINT PK_SurveyMarket PRIMARY KEY(surveyId, version)
);

-- SurveyMarketQuestionVersion
CREATE TABLE SurveyMarketQuestionVersion (
    surveyQuestionId INT NOT NULL REFERENCES SurveyQuestion(id),
    version TINYINT NOT NULL,
    isReanswerRequired BIT NOT NULL DEFAULT(0),
    referenceSurveyQuestionId INT NULL REFERENCES SurveyQuestion(id),
    CONSTRAINT PK_SurveyMarketQuestionVersion PRIMARY KEY(surveyQuestionId, version)
);

-- SurveyTakenResult
CREATE TABLE SurveyTakenResult (
    id INT IDENTITY PRIMARY KEY,
    surveyId INT NOT NULL REFERENCES Survey(id),
    takerId INT NOT NULL REFERENCES Account(id),
    isValid BIT NOT NULL,
    invalidReason NVARCHAR(500) NULL,
    moneyEarned DECIMAL(18,2) NULL,
    xpEarned INT NULL,
    completedAt DATETIME NOT NULL DEFAULT(GETDATE())
);

-- SurveyResponse
CREATE TABLE SurveyResponse (
    id INT IDENTITY PRIMARY KEY,
    surveyTakenResultId INT NOT NULL REFERENCES SurveyTakenResult(id),
    surveyQuestionId INT NOT NULL REFERENCES SurveyQuestion(id),
    isValid BIT NOT NULL,
    valueJsonString NVARCHAR(MAX) NOT NULL
);

-- SurveyMarketResponseVersion
CREATE TABLE SurveyMarketResponseVersion (
    surveyResponseId INT NOT NULL REFERENCES SurveyResponse(id),
    version TINYINT NOT NULL,
    CONSTRAINT PK_SurveyMarketResponseVersion PRIMARY KEY(surveyResponseId, version)
);

-- DataPurchase
CREATE TABLE DataPurchase (
    id INT IDENTITY PRIMARY KEY,
    buyerId INT NOT NULL REFERENCES Account(id),
    marketSurveyId INT NOT NULL REFERENCES Survey(id),
    version TINYINT NOT NULL,
    purchasedAt DATETIME NOT NULL DEFAULT(GETDATE())
);

-- DataPurchaseDetail
CREATE TABLE DataPurchaseDetail (
    dataPurchaseId INT NOT NULL REFERENCES DataPurchase(id),
    surveyResponseId INT NOT NULL REFERENCES SurveyResponse(id),
    CONSTRAINT PK_DataPurchaseDetail PRIMARY KEY(dataPurchaseId, surveyResponseId)
);

-- SurveyRewardTracking
CREATE TABLE SurveyRewardTracking (
    id INT IDENTITY PRIMARY KEY,
    surveyId INT NOT NULL REFERENCES Survey(id),
    rewardPrice DECIMAL(18,2) NOT NULL,
    rewardXp INT NOT NULL,
    createdAt DATETIME NOT NULL DEFAULT(GETDATE())
);

-- FilterTagType
CREATE TABLE FilterTagType (
    id INT PRIMARY KEY,
    name NVARCHAR(50) NOT NULL
);

-- FilterTag
CREATE TABLE FilterTag (
    id INT PRIMARY KEY,
    name NVARCHAR(250) NOT NULL,
    tagColor VARCHAR(50) NOT NULL,
    filterTagTypeId INT NOT NULL REFERENCES FilterTagType(id)
);

-- SurveyTakenResultTagFilter
CREATE TABLE SurveyTakenResultTagFilter (
    surveyTakenResultId INT NOT NULL REFERENCES SurveyTakenResult(id),
    additionalFilterTag INT NOT NULL REFERENCES FilterTag(id),
    summary NVARCHAR(MAX) NULL,
    CONSTRAINT PK_SurveyTakenResultTagFilter PRIMARY KEY(surveyTakenResultId, additionalFilterTag)
);

-- TakerTagFilter
CREATE TABLE TakerTagFilter (
    takerId INT NOT NULL REFERENCES Account(id),
    filterTagId INT NOT NULL REFERENCES FilterTag(id),
    summary NVARCHAR(MAX) NULL,
    CONSTRAINT PK_TakerTagFilter PRIMARY KEY(takerId, filterTagId)
);

-- SurveyTagFilter
CREATE TABLE SurveyTagFilter (
    surveyId INT NOT NULL REFERENCES Survey(id),
    filterTagId INT NOT NULL REFERENCES FilterTag(id),
    summary NVARCHAR(MAX) NULL,
    CONSTRAINT PK_SurveyTagFilter PRIMARY KEY(surveyId, filterTagId)
);

-- SurveyTopicFavorite
CREATE TABLE SurveyTopicFavorite (
    accountId INT NOT NULL REFERENCES Account(id),
    surveyTopicId INT NOT NULL REFERENCES SurveyTopic(id),
    favoriteScore TINYINT NOT NULL,
    CONSTRAINT PK_SurveyTopicFavorite PRIMARY KEY(accountId, surveyTopicId)
);

-- SurveyTakerSegment
CREATE TABLE SurveyTakerSegment (
    surveyId INT NOT NULL REFERENCES Survey(id),
    countryRegion NVARCHAR(250) NULL,
    maritalStatus NVARCHAR(250) NULL,
    averageIncome NVARCHAR(250) NULL,
    educationLevel NVARCHAR(250) NULL,
    jobField NVARCHAR(250) NULL,
    prompt NVARCHAR(MAX) NULL,
    tagFilterAccuracyRate FLOAT NULL
);

-- SurveyFeedback
CREATE TABLE SurveyFeedback (
    surveyId INT NOT NULL REFERENCES Survey(id),
    takerId INT NOT NULL REFERENCES Account(id),
    ratingScore FLOAT NOT NULL,
    comment NVARCHAR(500) NULL,
    createdAt DATETIME NOT NULL DEFAULT(GETDATE()),
    updatedAt DATETIME NOT NULL DEFAULT(GETDATE()),
    CONSTRAINT PK_SurveyFeedback PRIMARY KEY(surveyId, takerId)
);

-- PaymentType
CREATE TABLE PaymentType (
    id INT PRIMARY KEY,
    name NVARCHAR(50) NOT NULL,
    operationType NVARCHAR(20) NOT NULL
);

-- PaymentStatus
CREATE TABLE PaymentStatus (
    id INT PRIMARY KEY,
    name NVARCHAR(50) NOT NULL
);

-- PaymentHistory
CREATE TABLE PaymentHistory (
    id INT IDENTITY PRIMARY KEY,
    accountId INT NOT NULL REFERENCES Account(id),
    amount DECIMAL(18,2) NOT NULL,
    paymentTypeId INT NOT NULL REFERENCES PaymentType(id),
    paymentStatusId INT NOT NULL REFERENCES PaymentStatus(id),
    createdAt DATETIME NOT NULL DEFAULT(GETDATE())
);