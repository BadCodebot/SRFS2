#include "pch.h"
#include "Parity.h"
#include "GF16.h"

namespace ReedSolomon {

	Parity::Parity(size_t nDataCodewords, size_t nParityCodewords, size_t codewordsPerSlice) :
		_nParityCodewords(nParityCodewords), _nDataCodewords(nDataCodewords), _generator(nParityCodewords),
		_codewordsPerSlice(codewordsPerSlice), _multiplicationTable() {

		_parityBlocksPerVector = (_nParityCodewords + 7) / 8;
		_parityVectors = (uint16_t*)_aligned_malloc(_parityBlocksPerVector * 16 * nDataCodewords, 64);

		uint16_t* generator = _generator.GetCoefficients();

		size_t codewordsPerVector = _parityBlocksPerVector * 8;

		if (_nParityCodewords > 1) {

			uint16_t* currentVector = _parityVectors;
			uint16_t* previousVector = nullptr;

			// The first vector is just a copy of the generator
			for (size_t i = 0; i < _nParityCodewords; i++) currentVector[i] = generator[i];
			for (size_t i = _nParityCodewords; i < codewordsPerVector; i++) currentVector[i] = 0;

			for (size_t j = 1; j < nDataCodewords; j++) {
				previousVector = currentVector;
				currentVector += codewordsPerVector;

				uint16_t c = previousVector[_nParityCodewords - 1];
				currentVector[0] = GF16::Multiply(c, generator[0]);
				for (size_t i = 1; i < _nParityCodewords; i++) {
					currentVector[i] = GF16::Add(previousVector[i - 1], GF16::Multiply(c, generator[i]));
				}
				for (size_t i = _nParityCodewords; i < codewordsPerVector; i++) currentVector[i] = 0;
			}
		}
		else {
			uint16_t* currentVector = _parityVectors;
			for (size_t j = 0; j < nDataCodewords; j++) {
				currentVector[0] = 1;
				for (size_t i = 1; i < codewordsPerVector; i++) currentVector[i] = 0;
			}
			currentVector += codewordsPerVector;
		}

		_parity = (__m128i*)_aligned_malloc(_parityBlocksPerVector * 16 * _codewordsPerSlice, 64);
		Reset();
	}

	Parity::~Parity() {
		_aligned_free(_parityVectors);
	}

	void Parity::Reset() {
		memset(_parity, 0, _parityBlocksPerVector * 16 * _codewordsPerSlice);
	}

	void Parity::Calculate(char* data, size_t codewordIndex) {
		__m128i* parityBlock = (__m128i*)(_parityVectors + _parityBlocksPerVector * 8 * codewordIndex);
		__m128i* dest = _parity;
		for (size_t i = 0; i < _parityBlocksPerVector; i++) {
			_multiplicationTable.Set(*parityBlock);
			_multiplicationTable.MultiplyAndXor((uint16_t*)data, dest, _codewordsPerSlice);
			parityBlock++;
			dest += _codewordsPerSlice;
		}
	}

	Parity* Parity_Construct(size_t nDataCodewords, size_t nParityCodewords, size_t codewordsPerSlice) {
		return new Parity(nDataCodewords, nParityCodewords, codewordsPerSlice);
	}
}