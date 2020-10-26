
// FarNet plugin for Far Manager
// Copyright (c) Roman Kuzmin

#pragma once

namespace FarNet
{;
ref class History sealed : IHistory
{
public:
	virtual array<HistoryInfo^>^ Command() override;
	virtual array<HistoryInfo^>^ Dialog(String^ name) override;
	virtual array<HistoryInfo^>^ Editor() override;
	virtual array<HistoryInfo^>^ Folder() override;
	virtual array<HistoryInfo^>^ Viewer() override;
internal:
	static History Instance;
private:
	History() {}
};

}
