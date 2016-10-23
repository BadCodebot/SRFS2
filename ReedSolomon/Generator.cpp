#include "pch.h"
#include "Generator.h"
#include "GF16.h"

namespace ReedSolomon {

	Generator::Generator(int nParityCodewords) {

		_nParityCodewords = nParityCodewords;

		_coefficients = new uint16_t[nParityCodewords];

		for (int i = 0; i < nParityCodewords - 1; i++) _coefficients[i] = 0;
		_coefficients[nParityCodewords - 1] = 1;

		for (int j = 1; j < nParityCodewords; j++) {
			uint16_t scalar = GF16::Exp(j);
			_coefficients[nParityCodewords - j - 1] = GF16::Multiply(scalar, _coefficients[nParityCodewords - j]);
			for (int k = nParityCodewords - j; k < nParityCodewords - 1; k++)
				_coefficients[k] = GF16::Add(_coefficients[k], GF16::Multiply(scalar, _coefficients[k + 1]));
			_coefficients[nParityCodewords - 1] = GF16::Add(_coefficients[nParityCodewords - 1], scalar);
		}
	}

	Generator::~Generator() {
		delete[] _coefficients;
	}
}