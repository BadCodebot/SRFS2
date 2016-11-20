#include "stdafx.h"
#include "Repair.h"
#include "GF16.h"
#include <stdexcept>
#include <iostream>

namespace ReedSolomon {

	Repair::Repair(const Syndrome& rss, int nCodeWords, int* errorLocations, int errorCount)
		: _rss(rss), _nCodeWords(nCodeWords), _correctionMatrix(errorCount), errorCount(errorCount) {

		// Make sure we don't have too many errors
		if (errorCount > rss.GetNParityCodewords()) throw std::invalid_argument("Too many errors");

		// convert the error codeword indexes to the corresponding polynomial order
		errorOrders = new int[errorCount];
		for (int i = 0; i < errorCount; i++) errorOrders[i] = errorLocations[i];

		// Create the correction matrix
		for (int r = 0; r < errorCount; r++) {
			for (int c = 0; c < errorCount; c++) {
				_correctionMatrix[r][c] = GF16::Power(2, r * errorOrders[c]);
			}
		}

		_correctionMatrix.Invert();
	}

	Repair::~Repair() {
		delete[] errorOrders;
	}

	void Repair::Correction(int errorLocationOffset, uint16_t* data) const {
		for (int i = 0; i < _rss.GetCodewordsPerSlice(); i++) {
//			data[i] = 0;
			for (int j = 0; j < errorCount; j++) {
				data[i] = GF16::Add(data[i], GF16::Multiply(_rss.GetSyndrome(i, j), _correctionMatrix[errorLocationOffset][j]));
			}
		}
	}

	Repair* Repair_Construct(const Syndrome* rss, int nCodeWords, int* errorLocations, int errorCount) {
		return new Repair(*rss, nCodeWords, errorLocations, errorCount);
	}

	void Repair_Destruct(Repair* rsr) {
		delete rsr;
	}

	void Repair_Correction(Repair* rsr, int errorLocationOffset, uint16_t* data) {
		rsr->Correction(errorLocationOffset, data);
	}

	int Repair_GetNCodeWords(Repair* rsr) {
		return rsr->GetNCodeWords();
	}
}