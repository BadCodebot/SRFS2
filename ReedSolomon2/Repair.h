#pragma once
#include "Syndrome.h"
#include "SquareMatrix.h"

namespace ReedSolomon {

	class Repair {

	public:

		Repair(const Syndrome& rss, int nCodeWords, int* errorLocations, int errorCount);
		~Repair();

		inline int GetNCodeWords() const { return _nCodeWords; }

		void Correction(int errorLocationOffset, uint16_t* data) const;

	private:

		const Syndrome& _rss;
		int _nCodeWords;
		SquareMatrix _correctionMatrix;
		int* errorOrders;
		int errorCount;
	};

	extern "C" {
		__declspec(dllexport) Repair* Repair_Construct(const Syndrome* rss, int nCodeWords, int* errorLocations, int errorCount);
		__declspec(dllexport) void Repair_Destruct(Repair* rsr);
		__declspec(dllexport) void Repair_Correction(Repair* rsr, int errorLocationOffset, uint16_t* data);
		__declspec(dllexport) int Repair_GetNCodeWords(Repair* rsr);
	}
}